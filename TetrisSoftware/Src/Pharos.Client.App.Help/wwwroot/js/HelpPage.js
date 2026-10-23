export {
    startListeningToLinkClicks,
    stopListeningToLinkClicks
}

const startListeningToLinkClicks = (element, dotNetRef) => {
    if (!element || typeof element.addEventListener !== "function") {
        throw new Error("A valid element is required.");
    }

    if (!dotNetRef) {
        throw new Error("A DotNetObjectReference is required.");
    }

    stopListeningToLinkClicks(element);

    element._linkClickHandler = event => {
        const link = event.target.closest?.("a");
        const href = link?.getAttribute("href");

        if (href) {
            dotNetRef
                .invokeMethodAsync("OnLinkClicked", href)
                .catch(() => { });
        }
    };

    element.addEventListener("click", element._linkClickHandler);
};

const stopListeningToLinkClicks = element => {
    if (element?._linkClickHandler) {
        element.removeEventListener("click", element._linkClickHandler);
        delete element._linkClickHandler;
    }
};