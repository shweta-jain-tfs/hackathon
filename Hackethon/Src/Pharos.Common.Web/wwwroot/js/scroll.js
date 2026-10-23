export {
    scrollSelectorIntoView,
    scrollToElement
}

// Scroll to the element by ID, with optional scroll position control
// block: 'start' (top), 'center', 'end' (bottom), 'nearest' (default)
// behavior: 'auto' (instant) is safer than 'smooth' for Blazor components
const scrollToElement = (elementId, block = 'center', behavior = 'auto') => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ block: block, behavior: behavior });
    }
}

// Scroll matching elements into view if needed
const scrollSelectorIntoView = selector => {
    document
        .querySelectorAll(selector)
        .forEach(element => {
            element.scrollIntoView({
                block: 'nearest',
                behavior: 'auto'
            });
        });
}