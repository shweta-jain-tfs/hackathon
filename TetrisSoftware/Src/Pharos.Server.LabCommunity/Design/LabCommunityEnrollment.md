# LabCommunity Enrollment Process

```mermaid
sequenceDiagram
    actor User
    box LabCommunity module
      participant SettingsUI
      participant Application
      participant Database
    end
    participant LabCommunityAPI
    participant HTTPS

    User->>SettingsUI: Open LabCommunity settings
    SettingsUI->>User: Show form (Enable, Laboratory code, Username, Password)
    User->>SettingsUI: Enable LabCommunity and fill in the required fields
    SettingsUI->>Application: Save settings
    Application->>Application: LabCommunity enabled, trigger enrollment process
    alt Enrollment exists
        Application ->> Application: Skip enrollment, API key already stored
    end
    Application->>LabCommunityAPI: Send credentials to obtain API key
    alt Enrollment succeeds
        LabCommunityAPI-->>Application: Return ApiKey
        Application->>Database: Store ApiKey encrypted
    else Enrollment fails
        LabCommunityAPI-->>Application: Error or invalid credentials
    end

    %% Subsequent requests flow (user perspective)
    Application->>Application: Perform action that requires LabCommunity (e.g. send instrument status)
    Application->>Database: Retrieve ApiKey (decrypt in-memory)
    Application->>HTTPS: Build request, add header "X-API-Key: {ApiKey}" and send
    HTTPS->>LabCommunityAPI: Request with ApiKey header
    LabCommunityAPI->>LabCommunityAPI: Validate ApiKey hash
    alt ApiKey valid
        LabCommunityAPI-->>Application: Return data / success
    else ApiKey invalid or expired
        LabCommunityAPI-->>Application: 401 Unauthorized
    end
```

Notes:
- The user provides credentials only once; the application exchanges them for an API key from the LabCommunity service.
- The returned API key is stored encrypted at rest in the application's database and never logged in plaintext.
- The application attaches the API key to outgoing requests to the LabCommunity API so credentials are not repeatedly sent.
