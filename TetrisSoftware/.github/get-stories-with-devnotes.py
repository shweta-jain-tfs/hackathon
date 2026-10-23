import requests
import base64
import warnings
from bs4 import BeautifulSoup
import sys
import argparse
import os


import re

def extract_pbi_number_from_md(md_path):
    """Extract PBI number from copilot-instructions.md file"""
    with open(md_path, 'r', encoding='utf-8') as f:
        for line in f:
            # Look for the pattern "PBI# <number>" anywhere in the line
            match = re.search(r'PBI#\s*(\d+)', line)
            if match:
                return int(match.group(1))
    return None

def get_auth_headers():
    """Build auth headers from environment PAT.

    Uses ADO_PAT first, then AZDO_PAT as a fallback.
    """
    pat = os.getenv('ADO_PAT') or os.getenv('AZDO_PAT')
    if not pat:
        raise ValueError(
            "Missing Azure DevOps PAT. Set ADO_PAT (preferred) or AZDO_PAT in your environment."
        )

    encoded_pat = base64.b64encode(f':{pat}'.encode()).decode()
    return {
        'Accept': 'application/json',
        'Authorization': f'Basic {encoded_pat}'
    }

API_URL = "https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workitems/{}?api-version=7.0"
COMMENTS_URL = "https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workitems/{}/comments?api-version=7.0"
HISTORY_URL = "https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workitems/{}/updates?api-version=7.0"
REVISIONS_URL = "https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workitems/{}/revisions?api-version=7.0"
HEADERS = {}

# When True, include all comments regardless of is_meaningful_comment filters
# Default: False — only include comments that pass meaningfulness filters unless
# the user explicitly passes --include-all on the CLI.
INCLUDE_ALL = False


def is_meaningful_comment(text):
    """Filter out low-value comments like single numbers or very short text"""
    text = text.strip()
    
    # Remove trailing numbers that might be artifacts (like "...text 1" or "...text 2")
    import re
    text = re.sub(r'\s+\d+$', '', text).strip()
    
    # Filter out very short comments (less than 15 characters)
    if len(text) < 15:
        return False
    
    # Filter out comments that are just numbers
    if text.isdigit():
        return False
    
    # Filter out comments that are just a single word or character
    if len(text.split()) <= 2 and not any(keyword in text.lower() for keyword in ['dev notes', 'technical', 'analysis', 'note:', 'todo', 'bug', 'fix', 'issue']):
        return False
    
    # Filter out common low-value phrases
    low_value_phrases = [
        'validation is completed', 'moving this to done', 'moved to done',
        'completed', 'done', 'closed', 'resolved'
    ]
    
    text_lower = text.lower()
    if any(phrase in text_lower for phrase in low_value_phrases) and len(text) < 100:
        return False
    
    # Keep comments that contain meaningful keywords
    meaningful_keywords = [
        'dev notes', 'technical analysis', 'implementation', 'logic', 'algorithm',
        'requirement', 'specification', 'design', 'architecture', 'validation',
        'testing', 'bug', 'issue', 'fix', 'solution', 'approach', 'method',
        'api', 'database', 'query', 'request', 'response', 'handler', 'command',
        'event', 'workflow', 'process', 'scenario', 'condition', 'rule', 'code',
        'class', 'function', 'module', 'component', 'service', 'endpoint'
    ]
    
    if any(keyword in text_lower for keyword in meaningful_keywords):
        return True
    
    # Keep longer comments that might contain useful information
    if len(text) > 80:
        return True
    
    return False

def contains_devnotes(text):
    """Check if the comment contains 'DevNotes' keyword (case-insensitive)"""
    text_lower = text.lower()
    return 'devnotes' in text_lower or 'dev notes' in text_lower


def extract_devnotes_text(text):
    """If the comment contains a Dev Notes label (like 'Dev Notes:'), extract the content after the label.
    Otherwise return the whole text if it contains the keyword. Returns None if not a DevNotes entry."""
    if not text:
        return None
    # Remove HTML and normalize whitespace first
    t = BeautifulSoup(text, 'html.parser').get_text()
    t = t.strip()
    # Try to find 'Dev Notes' or 'DevNotes' possibly followed by ':' or '-' and capture the remainder
    m = re.search(r'(?i)\bdev\s*notes\s*[:\-]?\s*(.*)', t)
    if m:
        content = m.group(1).strip()
        # If there's nothing after the label, fall back to full text
        return clean_comment_text(content if content else t)
    # If label not found but contains keyword, return full text
    if contains_devnotes(t):
        return clean_comment_text(t)
    return None

def clean_comment_text(text):
    """Clean up comment text by removing trailing numbers and extra whitespace"""
    import re
    # Remove trailing numbers that might be artifacts
    text = re.sub(r'\s+\d+$', '', text).strip()
    # Remove multiple spaces
    text = re.sub(r'\s+', ' ', text)
    return text

def get_work_item_revisions(devops_id, limit=50):
    """Get work item revisions which may contain comments"""
    warnings.filterwarnings('ignore', 'Unverified HTTPS request')
    url = REVISIONS_URL.format(devops_id)
    response = requests.get(url, headers=HEADERS, verify=False)
    
    if response.status_code == 200:
        revisions_data = response.json()
        revisions = revisions_data.get('value', [])
        
        # Sort by revision date (newest first) and take more revisions to search through
        sorted_revisions = sorted(revisions, key=lambda x: x.get('fields', {}).get('System.ChangedDate', ''), reverse=True)
        recent_revisions = sorted_revisions[:limit]
        
        discussions = []
        for revision in recent_revisions:
            fields = revision.get('fields', {})
            changed_by = fields.get('System.ChangedBy', {}).get('displayName', 'Unknown') if isinstance(fields.get('System.ChangedBy'), dict) else fields.get('System.ChangedBy', 'Unknown')
            changed_date = fields.get('System.ChangedDate', '')
            
            # Look for any text fields that might contain comments with DevNotes
            for field_name, field_value in fields.items():
                if isinstance(field_value, str) and 'devnotes' in field_value.lower():
                    clean_text = BeautifulSoup(field_value, 'html.parser').get_text()
                    if is_meaningful_comment(clean_text) or INCLUDE_ALL:
                        discussions.append({
                            'text': clean_comment_text(clean_text),
                            'author': changed_by,
                            'date': changed_date,
                            'type': 'revision'
                        })
        
        return discussions
    else:
        return []

def get_work_item_history(devops_id, limit=50):
    """Get work item history/updates which may contain discussions"""
    warnings.filterwarnings('ignore', 'Unverified HTTPS request')
    url = HISTORY_URL.format(devops_id)
    print(f"DEBUG: Trying History API: {url}")
    response = requests.get(url, headers=HEADERS, verify=False)
    print(f"DEBUG: History API status: {response.status_code}")
    
    if response.status_code == 200:
        history_data = response.json()
        updates = history_data.get('value', [])
        print(f"DEBUG: Found {len(updates)} history updates")
        
        # Sort by revision date (newest first) and take more updates to search through
        sorted_updates = sorted(updates, key=lambda x: x.get('revisedDate', ''), reverse=True)
        recent_updates = sorted_updates[:limit]
        
        discussions = []
        for i, update in enumerate(recent_updates):
            print(f"DEBUG: Processing history update {i+1}")
            # Check if there are field changes that might contain discussions
            fields = update.get('fields', {})
            revised_by = update.get('revisedBy', {}).get('displayName', 'Unknown')
            revised_date = update.get('revisedDate', '')
            
            print(f"DEBUG: Update {i+1} field keys: {list(fields.keys())}")
            
            # Look for description or comment-like fields
            discussion_text = ""
            # Only consider the current/new comment text (newValue). Ignore oldValue (deleted history).
            if 'System.History' in fields:
                history_field = fields['System.History']
                if isinstance(history_field, dict) and 'newValue' in history_field:
                    raw_history = history_field.get('newValue')
                    if raw_history:
                        discussion_text = BeautifulSoup(raw_history, 'html.parser').get_text()
                        print(f"DEBUG: Found System.History newValue content length: {len(raw_history)} chars")
                        print(f"DEBUG: System.History raw content: {raw_history[:200]}...")
                        print(f"DEBUG: System.History cleaned content: {discussion_text[:200]}...")
                else:
                    # no current content for this history update (likely a deletion or metadata-only update)
                    pass
            
            # Look for other potential discussion fields
            for field_name, field_data in fields.items():
                if ('comment' in field_name.lower() or 'discussion' in field_name.lower() or 
                    'description' in field_name.lower() or 'note' in field_name.lower()):
                    print(f"DEBUG: Checking field '{field_name}' for discussions")
                    # Only accept newValue (current active text). Ignore oldValue (deleted/previous text).
                    if isinstance(field_data, dict) and 'newValue' in field_data:
                        additional_text = str(field_data['newValue'])
                        # Clean HTML from the field value
                        additional_text = BeautifulSoup(additional_text, 'html.parser').get_text()
                        if additional_text and len(additional_text.strip()) > 0:
                            discussion_text += " " + additional_text
                            print(f"DEBUG: Found content in field '{field_name}': {additional_text[:100]}...")
            
            # Only include updates that contain current/new textual content (skip deleted/old-only history).
            if not discussion_text or not discussion_text.strip():
                # nothing current to include
                continue

            # Now filter by meaningfulness unless INCLUDE_ALL is set (INCLUDE_ALL will not resurrect deleted entries)
            if is_meaningful_comment(discussion_text) or INCLUDE_ALL:
                discussions.append({
                    'text': clean_comment_text(discussion_text.strip()),
                    'author': revised_by,
                    'date': revised_date,
                    'type': 'history_update'
                })
                print(f"DEBUG: Added meaningful discussion from update {i+1}")
        
        print(f"DEBUG: Total meaningful discussions from history: {len(discussions)}")
        return discussions
    else:
        print(f"DEBUG: History API failed with status {response.status_code}")
        return []

def get_specific_comment(comment_url):
    """Get a specific comment using the commentVersionRef URL"""
    warnings.filterwarnings('ignore', 'Unverified HTTPS request')
    print(f"DEBUG: Trying specific comment URL: {comment_url}")
    
    try:
        response = requests.get(comment_url, headers=HEADERS, verify=False)
        print(f"DEBUG: Specific comment API status: {response.status_code}")
        
        if response.status_code == 200:
            comment_data = response.json()
            print(f"DEBUG: Specific comment response keys: {list(comment_data.keys()) if isinstance(comment_data, dict) else 'Not a dict'}")
            
            # Extract comment text
            text = comment_data.get('text', '')
            if text:
                clean_text = BeautifulSoup(text, 'html.parser').get_text()
                if is_meaningful_comment(clean_text) or INCLUDE_ALL:
                    created_by = comment_data.get('createdBy', {}).get('displayName', 'Unknown')
                    created_date = comment_data.get('createdDate', '')
                    
                    return [{
                        'text': clean_comment_text(clean_text),
                        'author': created_by,
                        'date': created_date,
                        'type': 'specific_comment'
                    }]
            return []
        else:
            print(f"DEBUG: Specific comment API failed: {response.status_code} - {response.text[:200]}...")
            return []
    except Exception as e:
        print(f"DEBUG: Error fetching specific comment: {e}")
        return []

def get_latest_comments(devops_id, limit=50):
    """Get the latest comments for a work item using multiple API approaches"""
    warnings.filterwarnings('ignore', 'Unverified HTTPS request')
    
    # Try the standard comments API first
    url = COMMENTS_URL.format(devops_id)
    print(f"DEBUG: Trying Comments API: {url}")
    response = requests.get(url, headers=HEADERS, verify=False)
    print(f"DEBUG: Comments API status: {response.status_code}")
    
    formatted_comments = []
    
    if response.status_code == 200:
        comments_data = response.json()
        print(f"DEBUG: Comments API response keys: {list(comments_data.keys()) if isinstance(comments_data, dict) else 'Not a dict'}")
        
        comments = comments_data.get('comments', [])
        if not comments:
            # Try alternative keys
            comments = comments_data.get('value', [])
        
        print(f"DEBUG: Found {len(comments)} comments in Comments API")
        
        # Sort by creation date (newest first) and take more comments to search through
        sorted_comments = sorted(comments, key=lambda x: x.get('createdDate', ''), reverse=True)
        latest_comments = sorted_comments[:limit]
        
        for comment in latest_comments:
            text = comment.get('text', '')
            # Remove HTML tags if present
            clean_text = BeautifulSoup(text, 'html.parser').get_text()
            
            # Include all meaningful comments (we'll filter for DevNotes later)
            if is_meaningful_comment(clean_text) or INCLUDE_ALL:
                created_by = comment.get('createdBy', {}).get('displayName', 'Unknown')
                created_date = comment.get('createdDate', '')
                
                formatted_comments.append({
                    'text': clean_comment_text(clean_text),
                    'author': created_by,
                    'date': created_date,
                    'type': 'comment'
                })
    
    # Try alternative discussion API endpoints
    discussion_urls = [
        f"https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workItems/{devops_id}/discussions?api-version=7.0",
        f"https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/discussion/threads?workItemId={devops_id}&api-version=7.0",
        f"https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_apis/wit/workItems/{devops_id}?$expand=all&api-version=7.0"
    ]
    
    for i, disc_url in enumerate(discussion_urls):
        print(f"DEBUG: Trying discussion API {i+1}: {disc_url}")
        try:
            disc_response = requests.get(disc_url, headers=HEADERS, verify=False)
            print(f"DEBUG: Discussion API {i+1} status: {disc_response.status_code}")
            
            if disc_response.status_code == 200:
                disc_data = disc_response.json()
                print(f"DEBUG: Discussion API {i+1} response keys: {list(disc_data.keys()) if isinstance(disc_data, dict) else 'Not a dict'}")
                
                # Look for discussion-related data
                if 'discussions' in disc_data:
                    discussions = disc_data['discussions']
                    print(f"DEBUG: Found {len(discussions)} discussions in API {i+1}")
                elif 'value' in disc_data and isinstance(disc_data['value'], list):
                    discussions = disc_data['value']
                    print(f"DEBUG: Found {len(discussions)} items in value array for API {i+1}")
                elif i == 2:  # The expanded work item API
                    # Check if there are any expanded fields with discussion data
                    print(f"DEBUG: Expanded work item fields: {list(disc_data.get('fields', {}).keys())}")
                    continue
                else:
                    continue
                
                # Process discussions
                for discussion in discussions:
                    if isinstance(discussion, dict):
                        text = discussion.get('text', '') or discussion.get('content', '')
                        if text:
                            clean_text = BeautifulSoup(text, 'html.parser').get_text()
                            if is_meaningful_comment(clean_text) or INCLUDE_ALL:
                                author = discussion.get('author', {}).get('displayName', 'Unknown')
                                date = discussion.get('createdDate', '') or discussion.get('lastUpdatedDate', '')
                                
                                formatted_comments.append({
                                    'text': clean_comment_text(clean_text),
                                    'author': author,
                                    'date': date,
                                    'type': f'discussion_api_{i+1}'
                                })
        except Exception as e:
            print(f"DEBUG: Error with discussion API {i+1}: {e}")
    
    print(f"DEBUG: Total comments found from all APIs: {len(formatted_comments)}")
    return formatted_comments

def get_story_details(devops_id):
    warnings.filterwarnings('ignore', 'Unverified HTTPS request')
    url = API_URL.format(devops_id)
    response = requests.get(url, headers=HEADERS, verify=False)
    if response.status_code == 200:
        fields = response.json().get('fields', {})
        title = fields.get('System.Title', 'No title found')
        system_description_html = fields.get('System.Description', 'No description found')
        acceptance_criteria_html = fields.get('Microsoft.VSTS.Common.AcceptanceCriteria', 'No acceptance criteria found')
        
        # Check for DevNotes in all fields (removed debug output)
        for field_name, field_value in fields.items():
            if field_value and isinstance(field_value, str) and 'devnotes' in field_value.lower():
                pass  # Found DevNotes but not printing debug info
        
        # Check for commentVersionRef which might contain discussion references
        response_json = response.json()
        specific_comments = []
        if 'commentVersionRef' in response_json:
            comment_ref = response_json['commentVersionRef']
            print(f"DEBUG: Found commentVersionRef: {comment_ref}")
            if 'url' in comment_ref:
                specific_comments = get_specific_comment(comment_ref['url'])
        
        # Check all top-level response fields for potential discussion data
        print(f"DEBUG: Work item response keys: {list(response_json.keys())}")
        if 'relations' in response_json:
            print(f"DEBUG: Found {len(response_json['relations'])} relations")
            for i, relation in enumerate(response_json['relations'][:5]):  # Check first 5 relations
                print(f"DEBUG: Relation {i+1}: {relation.get('rel', 'unknown')} -> {relation.get('url', 'no url')}")
        
        system_description = BeautifulSoup(system_description_html, 'html.parser').get_text()
        acceptance_criteria = BeautifulSoup(acceptance_criteria_html, 'html.parser').get_text()
        
        # Check if DevNotes is in the description or acceptance criteria
        devnotes_in_description = contains_devnotes(system_description)
        devnotes_in_criteria = contains_devnotes(acceptance_criteria)

        def scan_for_devnotes_in_obj(obj):
            """Recursively scan a dict/list/str for DevNotes occurrences and return list of extracted texts"""
            found = []
            if obj is None:
                return found
            if isinstance(obj, str):
                text = BeautifulSoup(obj, 'html.parser').get_text()
                if contains_devnotes(text):
                    extracted = extract_devnotes_text(text)
                    if extracted:
                        found.append(extracted.strip())
                return found
            if isinstance(obj, dict):
                for k, v in obj.items():
                    found.extend(scan_for_devnotes_in_obj(v))
                return found
            if isinstance(obj, list):
                for item in obj:
                    found.extend(scan_for_devnotes_in_obj(item))
                return found
            # other types (int, bool, etc.) ignored
            return found
        
        # Get all comments and history
        latest_comments = get_latest_comments(devops_id, limit=50)
        history_discussions = get_work_item_history(devops_id, limit=50)
        revision_discussions = get_work_item_revisions(devops_id, limit=50)
        
        print(f"DEBUG: PBI {devops_id} - Comments: {len(latest_comments)}, History: {len(history_discussions)}, Revisions: {len(revision_discussions)}, Specific: {len(specific_comments)}")
        
        # Combine all comments and history discussions, including specific comments
        all_discussions = latest_comments + history_discussions + revision_discussions + specific_comments
        
        # Extract and combine DevNotes content from all discussions/comments
        devnotes_items = []
        seen_texts = set()
        for comment in all_discussions:
            raw_text = comment.get('text', '') or ''
            if INCLUDE_ALL:
                # Include any comment text (including short or blank) as DevNotes when requested
                extracted = raw_text
            else:
                extracted = extract_devnotes_text(raw_text)
            # If extracted is empty, try scanning the full comment object for DevNotes text
            if not extracted or not str(extracted).strip():
                try:
                    scanned = scan_for_devnotes_in_obj(comment)
                    if scanned:
                        # join multiple found pieces
                        extracted = '\n\n'.join([s for s in scanned if s and s.strip()])
                except Exception:
                    extracted = extracted

            # Skip empty extracted strings (avoid placeholders/provenance-only entries)
            if extracted is None or not str(extracted).strip():
                continue

            clean_text = str(extracted).strip()
            # Skip numeric-only artifacts and very short noise
            if re.fullmatch(r"\d+", clean_text):
                continue
            if len(clean_text) < 3:
                continue
            clean_key = clean_text.lower()
            if clean_key not in seen_texts:
                # keep provenance info
                author = comment.get('author', 'Unknown')
                date = comment.get('date', '')
                devnotes_items.append({
                    'text': clean_text,
                    'author': author,
                    'date': date,
                    'source_type': comment.get('type', '')
                })
                seen_texts.add(clean_key)

        # Also check description and acceptance criteria for DevNotes and include them
        desc_extracted = extract_devnotes_text(system_description)
        if desc_extracted:
            key = desc_extracted.strip().lower()
            if key not in seen_texts:
                devnotes_items.append({
                    'text': desc_extracted.strip(),
                    'author': 'Description',
                    'date': '',
                    'source_type': 'description'
                })
                seen_texts.add(key)

        crit_extracted = extract_devnotes_text(acceptance_criteria)
        if crit_extracted:
            key = crit_extracted.strip().lower()
            if key not in seen_texts:
                devnotes_items.append({
                    'text': crit_extracted.strip(),
                    'author': 'AcceptanceCriteria',
                    'date': '',
                    'source_type': 'acceptance_criteria'
                })
                seen_texts.add(key)

        # Scan the full response JSON for any DevNotes text in nested structures
        scanned = scan_for_devnotes_in_obj(response_json)
        for s in scanned:
            key = s.strip().lower()
            if key and key not in seen_texts:
                devnotes_items.append({
                    'text': s.strip(),
                    'author': 'NestedField',
                    'date': '',
                    'source_type': 'nested_field'
                })
                seen_texts.add(key)

        # Sort DevNotes items by date (newest first) if dates are present
        devnotes_items = sorted(devnotes_items, key=lambda x: x.get('date', ''), reverse=True)

        # Combine multiple DevNotes into a single block (preserve authors/dates)
        combined_devnotes = ''
        if devnotes_items:
            parts = []
            for item in devnotes_items:
                text = (item.get('text') or '').strip()
                # Only include the DevNotes content itself (no author/date headers)
                if text:
                    parts.append(text)
            combined_devnotes = '\n\n'.join(parts)
        
        # Sort all discussions by date for console output
        all_discussions_sorted = sorted(all_discussions, key=lambda x: x.get('date', ''), reverse=True)
    # JSON dump disabled: this script will not write workitem/updates/revisions JSON files.
    # (Previously optional dump code removed to only produce ghcp/Story-<PBI>.md)
        
        return {
            'id': devops_id,
            'title': title,
            'system_description': system_description,
            'acceptance_criteria': acceptance_criteria,
            'devnotes_items': devnotes_items,
            'devnotes_combined': combined_devnotes,
            'all_comments': all_discussions_sorted  # Add all comments for console display
        }
    else:
        return {
            'id': devops_id,
            'title': 'Error',
            'system_description': f'Failed to retrieve (status {response.status_code})',
            'acceptance_criteria': '',
            'devnotes_items': [],
            'devnotes_combined': '',
            'all_comments': []
        }

def main():
    parser = argparse.ArgumentParser(description='Fetch DevOps story and extract DevNotes')
    parser.add_argument('--pbi', type=int, help='PBI number to fetch')
    parser.add_argument('--include-all', action='store_true', help='Include all comments (even short or non-meaningful)')
    args = parser.parse_args()

    pbi_number = None
    global HEADERS

    try:
        HEADERS = get_auth_headers()
    except ValueError as ex:
        print(str(ex))
        print("PowerShell example: $env:ADO_PAT = '<your_pat>'")
        return

    # Prefer explicit CLI argument
    global INCLUDE_ALL
    if getattr(args, 'include_all', False) or getattr(args, 'include-all', False):
        INCLUDE_ALL = True
    # no-op: --dump-json removed

    if args.pbi:
        pbi_number = args.pbi
    else:
        # Try extracting PBI from the story-implementation.md prompt file
        md_file = os.path.join(os.path.dirname(__file__), 'story-implementation.md')
        if not os.path.exists(md_file):
            # fallback to copilot-instructions.md for backward compatibility
            md_file = os.path.join(os.path.dirname(__file__), 'copilot-instructions.md')
        pbi_number = extract_pbi_number_from_md(md_file)

    if not pbi_number:
        print("No PBI number found. Provide --pbi <number> or include 'PBI# <number>' in story-implementation.md")
        return

    print(f"Found PBI number: {pbi_number}")
    story_ids = [pbi_number]
    
    results = []
    not_found = []
    for devops_id in story_ids:
        details = get_story_details(devops_id)
        if details['title'] == 'Error':
            not_found.append(devops_id)
        results.append(details)

    # Prepare content in Story.md format
    lines = []
    updated_ids = []
    for story in results:
        if story['title'] != 'Error':
            lines.append(f"# {story['id']}-{story['title']}")
            lines.append(f"    - Description-{story['system_description']}")
            lines.append(f"    - AcceptanceCriteria-{story['acceptance_criteria']}")

            # Add combined DevNotes block
            if story.get('devnotes_combined'):
                lines.append("    - DevNotes:")
                # Indent the combined block for readability
                for devline in story['devnotes_combined'].splitlines():
                    lines.append(f"        {devline}")
            else:
                lines.append(f"    - DevNotes: No DevNotes comments found")

            lines.append("")
            updated_ids.append(str(story['id']))

    # Print DevNotes summary only (removed duplicate ALL COMMENTS section)
    print("\n=== DEVNOTES SUMMARY ===")
    for story in results:
        if story['title'] != 'Error':
            devcount = len(story.get('devnotes_items', []))
            print(f"\nStory {story['id']}: Found {devcount} DevNotes comments")
            if devcount:
                for i, comment in enumerate(story.get('devnotes_items', []), 1):
                    print(f"  DevNote {i}: {comment['text']}")
            else:
                print("  No DevNotes comments found")
    print("=" * 60)

    # Ensure ghcp folder exists and write to ghcp/Story-<PBI Number>.md (overwrite)
    out_dir = os.path.join(os.path.dirname(__file__), '..', '..', 'ghcp')
    out_dir = os.path.normpath(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    output_filename = os.path.join(out_dir, f'Story-{pbi_number}.md')
    with open(output_filename, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines).strip() + '\n')

    if updated_ids:
        print(f"The following stories were updated in {output_filename}: {', '.join(updated_ids)}")
    else:
        print(f"No valid stories were updated in {output_filename}.")
    if not_found:
        print(f"The following stories could not be found or retrieved: {', '.join(str(i) for i in not_found)}. Please check these IDs in DevOps.")

if __name__ == "__main__":
    main()
