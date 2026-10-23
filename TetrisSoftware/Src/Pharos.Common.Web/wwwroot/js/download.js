export {
    downloadFromStreamReference,
    downloadFromStreamReferenceWithDialog,
    downloadFromUrl,
    checkExportDialog,
    exportIfSavePressed
}
let exportFileHandle = null;

/**
 * Allows the user to select the destination folder and change the file name using the browser save file dialog.
 * Falls back to a standard download if the File System API is not supported.
 * showSaveFilePicker returns a error AbortError on cancel
 * @async
 * @param {string} fileName - The suggested file name for the download.
 * @param {any} dotNetStreamReference - The .NET stream reference containing the file data.
 * @param {string} [type="csv"] - The file type ("csv" or "zip").
 * @throws {Error} - Rethrows any error except AbortError.
 * @returns {Promise<void>} A promise that resolves when the download is complete.
 */
const downloadFromStreamReferenceWithDialog = async (fileName, dotNetStreamReference, type = "csv") => {
    if (!supportsFileSystemApi()) {
        console.warn("File System API not supported by browser.");
        await downloadFromStreamReference(fileName, dotNetStreamReference);
        return;
    }

    try {
        const fileHandle = await showSaveFilePicker(generateFilePickerOptions(fileName, type));
        await handleWriteStream(fileHandle, dotNetStreamReference);
    } catch (err) {
        if (err.name !== "AbortError") {
            console.error(`${err.name}: ${err.message}`);
            throw err;
        }
    }
};

/**
 * Exports file only when the user presses Save in the dialog.
 * Uses the previously stored file handle.
 * Falls back to a standard download if the File System API is not supported.
 * @async
 * @param {string} fileName - The file name to use if fallback is needed.
 * @param {any} dotNetStreamReference - The .NET stream reference containing the file data.
 * @returns {Promise<void>} A promise that resolves when the export is complete.
 */
const exportIfSavePressed = async (fileName, dotNetStreamReference) => {
    if (!supportsFileSystemApi()) {
        console.warn("File System API not supported by browser.");
        return downloadFromStreamReference(fileName, dotNetStreamReference);
    }
    return handleWriteStream(exportFileHandle, dotNetStreamReference);
};

/**
 * Prompts the user with a save file dialog for exporting the file.
 * Stores the file handle for later use if the user confirms.
 * showSaveFilePicker returns a error AbortError on cancel
 * @async
 * @param {string} fileName - The suggested file name for the export.
 * @param {string} [type="csv"] - The file type ("csv" or "zip").
 * @returns {Promise<boolean>} - Resolves to true if the user pressed Save, false if Cancel or an error occurred.
 * @throws {Error} - Rethrows any error except AbortError.
 */
const checkExportDialog = async (fileName, type = "csv") => {
    try {
        exportFileHandle = await showSaveFilePicker(generateFilePickerOptions(fileName, type));
        return true;
    } catch (err) {
        if (err.name !== "AbortError") {
            console.error(`${err.name}: ${err.message}`);
            throw err;
        }
        return false;
    }
};

/**
 * Handles writing a stream from a .NET stream reference to a file using the File System Access API.
 * @param {FileSystemFileHandle} fileHandle - The file handle to write to.
 * @param {any} dotNetStreamReference - The .NET stream reference containing the file data.
 * @returns {Promise<void>} A promise that resolves when the write is complete.
 */
const handleWriteStream = async (fileHandle, dotNetStreamReference) => {
    const [writableStream, arrayBuffer] = await Promise.all([
        fileHandle.createWritable(),
        dotNetStreamReference.arrayBuffer()
    ]);
    await writableStream.write(arrayBuffer);
    await writableStream.close();
};

/**
 * Triggers a file download in the browser using a .NET stream reference.
 * @param {string} fileName - The name for the downloaded file.
 * @param {any} dotNetStreamReference - The .NET stream reference containing the file data.
 * @returns {Promise<void>} A promise that resolves when the download is triggered.
 */
const downloadFromStreamReference = async (fileName, dotNetStreamReference) => {
    const arrayBuffer = await dotNetStreamReference.arrayBuffer();
    var blob = new Blob([arrayBuffer], { type: "application/octet-stream" });
    const url = URL.createObjectURL(blob);
    triggerDownload(fileName, url);
    URL.revokeObjectURL(url);
};

/**
 * Triggers a file download in the browser from a URL.
 * @param {string} fileName - The name to use for the downloaded file.
 * @param {string} fileUrl - The URL of the file to download.
 */
const downloadFromUrl = async (fileName, fileUrl) => {
    triggerDownload(fileName, fileUrl);
};

/**
 * Triggers a file download in the browser by creating a temporary anchor element.
 * @param {string} fileName - The name for the downloaded file.
 * @param {string} url - The object URL for the file blob.
 */
const triggerDownload = (fileName, url) => {
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName ?? "download.csv";
    a.style.display = "none";
    document.body.appendChild(a);
    a.click();
    a.remove();
};

/**
 * Checks if the File System Access API is supported and the window is topmost.
 * @returns {boolean} True if supported, otherwise false.
 */
const supportsFileSystemApi = () => "showSaveFilePicker" in window && isTopmostWindow();

/**
 * Checks if the current window is the topmost window.
 * @returns {boolean} True if the current window is topmost, otherwise false.
 */
const isTopmostWindow = () => {
    try {
        return window.self === window.top;
    } catch {
        return false;
    }
};

/**
 * Generates file picker options for the save file dialog based on file type.
 * @param {string} fileName - The suggested file name.
 * @param {string} type - The file type ("csv" or "zip").
 * @returns {object} The file picker options object.
 */
const generateFilePickerOptions = (fileName, type) => {
    const opts = {
        suggestedName: fileName ?? "",
        excludeAcceptAllOption: true,
        types: []
    };

    if (type === "csv") {
        opts.types.push({
            accept: { "text/csv": [".csv"] }
        });
    } else if (type === "zip") {
        opts.types.push({
            accept: { "application/zip": [".zip"] }
        });
    }

    return opts;
};
