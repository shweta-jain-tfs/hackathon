export {
    saveScrollPosition,
    scrollActiveItemIntoView,
}

const localStorageKey = "PhSubnavMenu_scroll";

const saveScrollPosition = (scrollElement) => localStorage.setItem(localStorageKey, scrollElement?.scrollTop ?? 0);

const scrollActiveItemIntoView = (scrollElement, activeItemSelector) => {
    const activeElement = scrollElement?.querySelector(`:scope ${activeItemSelector}`);
    if (!activeElement) {
        return;
    }

    // scroll to saved scroll position
    scrollElement.scrollTop = localStorage.getItem(localStorageKey);

    // if active element is not in view, scroll it into view
    activeElement.scrollIntoView({
        behavior: "instant",
        block: "nearest",
    });

    saveScrollPosition(scrollElement);
};
