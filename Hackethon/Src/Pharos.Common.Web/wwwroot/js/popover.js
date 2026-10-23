export {
    hidePopover,
    showPopover,
}

// Close an element that has the popover attribute.
const hidePopover = (element) => {
    try {
        element?.hidePopover?.();
    } catch (InvalidStateError) {
    }
}

// Open an element that has the popover attribute.
// Note: avoid using this in click events on toggle buttons since it will conflict with the soft dismiss of the popover.
const showPopover = (element) => {
    try {
        element?.showPopover?.();
    } catch (InvalidStateError) {
    }
}

// Note: togglePopover only works with elements that have popover="manual", which disables soft dismiss.
