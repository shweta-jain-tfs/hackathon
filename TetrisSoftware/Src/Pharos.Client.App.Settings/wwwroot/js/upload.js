export {
    uploadFileSelectedWithDialog,
}

const uploadFileSelectedWithDialog = async () => {
    if (!supportsFileSystemApi) {
        console.warn("File System API not supported by browser.");
        return;
    }

    let file = undefined;
    try {
        const [fileHandle] = await showOpenFilePicker({
            multiple: false,
            types: [
                {
                    description: 'CSV files',
                    accept: {
                        'text/csv': ['.csv'],
                    },
                },
            ],
        });
        file = await fileHandle.getFile();

        // Validate file type
        const fileName = file.name.toLowerCase();
        const isCsv = fileName.endsWith('.csv');

        if (!isCsv) {
            return "";
        }

        // Read the file content
        const arrayBuffer = await file.arrayBuffer();
        const fileContent = new TextDecoder().decode(arrayBuffer);
        return fileContent;
    } catch (err) {
        if (err.name !== "AbortError") {
            console.error(err.name, err.message);
            throw err;
        }
    }
};

const supportsFileSystemApi = () => "showOpenFilePicker" in window && isTopmostWindow();

const isTopmostWindow = () => {
    try {
        return window.self === window.top;
    } catch {
        return false;
    }
};