export {
    showModal,
    closeModal,
}

const preventEscape = (elem) => {
    if (!elem || elem.__escapeHandlerAttached) {
        return;
    }

    // note: this only works when focus is in the dialog
    elem.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            e.stopPropagation();
            e.preventDefault();
        }
    }, true);
    elem.__escapeHandlerAttached = true;
};

const showModal = (modalRef, preventClosingWithEscapeKey) => {
    modalRef?.showModal?.();
    if (preventClosingWithEscapeKey) {
        preventEscape(modalRef);
    }
};

const closeModal = (modalRef) => {
    modalRef?.close?.();
};
