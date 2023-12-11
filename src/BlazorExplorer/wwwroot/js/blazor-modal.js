// resharper disable all

function blazorInitializeModal(dialog, reference) {
    dialog.addEventListener("close", async e => {
        await reference.invokeMethodAsync("OnClose", dialog.returnValue);
    });
}

function blazorOpenModal(dialog) {
    if (!dialog.open) {
        dialog.showModal();
    }
}

function blazorCloseModal(dialog) {
    if (dialog.open) {
        dialog.close();
    }
}

function blazorCloseModalForce() {
    /* this does not use a named parameter but goes directly to a(?) dialog element */
    dialog.close();
    dialog.close();
}
