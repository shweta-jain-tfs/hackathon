export {
    getScrollPositionWithVisibility,
    scrollToWhenReady,
    scrollToHighlightedRow,
    scrollToRowIndex,
}

// CSS selector for highlighted rows
const HIGHLIGHTED_ROW_SELECTOR = '.ph-datagrid__row--highlighted';

/**
 * Gets the current scroll position and visibility state of an element
 * @param {HTMLElement} element - The element to get scroll position from
 * @returns {{position: number, isVisible: boolean}} Object containing scroll position and visibility
 */
const getScrollPositionWithVisibility = (element) => {
    if (!element) {
        return { position: 0, isVisible: false };
    }

    return {
        position: element.scrollTop,
        isVisible: element.checkVisibility()
    };
}

/**
 * Scrolls an element to a specific Y position when the DOM is ready
 * @param {HTMLElement} element - The element to scroll
 * @param {number} scrollY - The Y position to scroll to
 */
const scrollToWhenReady = (element, scrollY) => {
    if (!element || scrollY <= 0) {
        return;
    }

    // Double requestAnimationFrame ensures layout is ready.
    // The second frame covers calls made before OnAfterRenderAsync.
    requestAnimationFrame(() =>
        requestAnimationFrame(() => {
            element.scrollTo(0, scrollY);
        })
    );
}

/**
 * Scrolls to the highlighted row in the data grid
 * @param {HTMLElement} gridContainer - The grid container element to scroll
 */
const scrollToHighlightedRow = (gridContainer) => {
    if (!gridContainer) {
        return;
    }

    // Use requestAnimationFrame to ensure DOM is fully rendered
    requestAnimationFrame(() => {
        const highlightedRow = gridContainer.querySelector(HIGHLIGHTED_ROW_SELECTOR);

        if (highlightedRow) {
            highlightedRow.scrollIntoView({
                behavior: 'smooth',
                block: 'center',
                inline: 'nearest'
            });
        }
    });
}

/**
 * Scrolls to a specific row index in a virtualized grid and centers it.
 * Waits for rows to be available, then calculates scroll position based on row index.
 * 
 * @param {string|HTMLElement} gridContainerOrSelector - The grid container element or CSS selector
 * @param {number} targetRowIndex - The zero-based index of the row to scroll to and center
 * @param {Object} options - Configuration options
 * @param {number} [options.maxWaitMs=5000] - Maximum time to wait for rows to appear
 * @param {number} [options.rowHeight=56] - Row height in pixels (auto-detected if rows exist)
 * @returns {Promise<boolean>} - True if scroll succeeded, false if rows never appeared
 */
const scrollToRowIndex = async (gridContainerOrSelector, targetRowIndex, options = {}) => {
    const {
        maxWaitMs = 5000,
        rowHeight = 56
    } = options;

    console.log(`[scrollToRowIndex] Starting - targetRowIndex: ${targetRowIndex}`);

    // Get grid container
    let gridContainer;
    if (typeof gridContainerOrSelector === 'string') {
        gridContainer = document.querySelector(gridContainerOrSelector);
    } else {
        gridContainer = gridContainerOrSelector;
    }

    if (!gridContainer?.isConnected) {
        console.error('[scrollToRowIndex] Grid container not found or not connected');
        return false;
    }

    // STEP 1: Wait for rows to be available
    const hasRows = await waitForRows(gridContainer, maxWaitMs);
    if (!hasRows) {
        console.error('[scrollToRowIndex] Grid never populated with rows');
        return false;
    }

    // STEP 2: Calculate actual row height from DOM
    let actualRowHeight = rowHeight;
    const firstRow = gridContainer.querySelector('tbody tr');
    if (firstRow) {
        actualRowHeight = firstRow.offsetHeight;
        console.log(`[scrollToRowIndex] Detected row height: ${actualRowHeight}px`);
    }

    // STEP 3: Calculate scroll position to center the row
    const viewportHeight = gridContainer.clientHeight;
    const targetRowTop = targetRowIndex * actualRowHeight;
    const scrollPosition = Math.max(0, targetRowTop - (viewportHeight / 2) + (actualRowHeight / 2));

    console.log(`[scrollToRowIndex] Scrolling to position: ${scrollPosition}px`);

    // STEP 4: Execute smooth scroll
    gridContainer.scrollTo({
        top: scrollPosition,
        behavior: 'smooth'
    });

    // Trigger scroll event for virtualization
    gridContainer.dispatchEvent(new Event('scroll'));

    console.log('[scrollToRowIndex] Scroll completed');
    return true;
};

/**
 * Waits for grid rows to be available in the DOM
 * @param {HTMLElement} gridContainer - The grid container element
 * @param {number} maxWaitMs - Maximum time to wait in milliseconds
 * @returns {Promise<boolean>} - True if rows found, false if timeout
 */
const waitForRows = async (gridContainer, maxWaitMs) => {
    console.log('[waitForRows] Waiting for grid to populate with rows');

    const startTime = Date.now();
    const checkInterval = 50; // Check every 50ms

    while (Date.now() - startTime < maxWaitMs) {
        const rows = gridContainer.querySelectorAll('tbody tr');
        if (rows.length > 0) {
            const elapsedTime = Date.now() - startTime;
            console.log(`[waitForRows] Grid populated with ${rows.length} rows after ${elapsedTime}ms`);
            return true;
        }

        await new Promise(resolve => setTimeout(resolve, checkInterval));
    }

    console.error(`[waitForRows] Timeout: Grid still has 0 rows after ${maxWaitMs}ms`);
    return false;
};
