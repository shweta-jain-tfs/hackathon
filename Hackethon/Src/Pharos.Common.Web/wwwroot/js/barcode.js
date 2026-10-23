export {
    hasInputFocused,
    fillActiveInput
}

/**
 * Checks if any input element currently has focus 
 * @returns {boolean} True if an input/textarea has focus
 */
const hasInputFocused = () => {
    const activeElement = document.activeElement;
    if (!activeElement) return false;

    const tagName = activeElement.tagName.toUpperCase();
    const isInput = tagName === 'INPUT' &&
        activeElement.type !== 'button' &&
        activeElement.type !== 'submit' &&
        activeElement.type !== 'reset' &&
        activeElement.type !== 'checkbox' &&
        activeElement.type !== 'radio';
    const isTextarea = tagName === 'TEXTAREA';

    return isInput || isTextarea;
};

/**
 * Fills the currently focused input with text
 * @param {string} text - The text to insert
 * @returns {boolean} True if successful
 */
const fillActiveInput = (text) => {
    const activeElement = document.activeElement;
    if (!activeElement) return false;

    if (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA') {
        activeElement.value = text;

        // Trigger input and change events for Blazor framework detection
        const inputEvent = new Event('input', { bubbles: true });
        const changeEvent = new Event('change', { bubbles: true });
        activeElement.dispatchEvent(inputEvent);
        activeElement.dispatchEvent(changeEvent);

        return true;
    }
    return false;
};
