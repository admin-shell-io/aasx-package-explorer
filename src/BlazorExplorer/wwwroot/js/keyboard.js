// see: https://blog.ppedv.de/post/Blazor-Navbar-Top-Menu-auf-Bootstrap-Basis

// resharper disable all

// these functions are used ..

window.customShiftState = false;
window.customCtrlState = false;

window.mainLayoutAttachKeyboard = () => {

    // "promise" for activating Blazor
    window.customNetHandleKey = (sessionId, itemName) => {
        DotNet.invokeMethodAsync('BlazorExplorer', 'MainLayoutNetHandleKey', sessionId, itemName)
            .then(data => {
                console.log(data);
            });
    };

    // Keyboard handlers
    document.addEventListener('keydown', (event) => {
        const keyName = event.key;

        if (keyName === 'Control') {
            window.customCtrlState = true;
            return;
        }

        if (keyName === 'Shift') {
            window.customShiftState = true;
            return;
        }

        if (false && dialog && dialog.open && keyName === 'Enter' && !event.ctrlKey) {

            event.preventDefault();

            // alert("Enter!");

            //var els = dialog.getElementsByClassName("btn-enter");
            //if (els && els.length > 0)
            //    els[0].trigger("click");

            window.customNetHandleKey(window.mainLayoutSessionId, "@@ENTER@@");

            return;
        }

        if (window.mainLayoutHotkeys)
            window.mainLayoutHotkeys.forEach((hk) => {
                if (keyName.toLowerCase() === hk.key.toLowerCase()
                    && event.shiftKey === hk.isShift
                    && event.ctrlKey === hk.isCtrl
                    && event.altKey === hk.isAlt) {
                    event.preventDefault();
                    // alert("Found " + hk.itemName);
                    window.customNetHandleKey(window.mainLayoutSessionId, hk.itemName);
                }
            });

    }, false);

    document.addEventListener('keyup', (event) => {
        const keyName = event.key;

        if (keyName === 'Control') {
            window.customCtrlState = false;
            return;
        }

        if (keyName === 'Shift') {
            window.customShiftState = false;
            return;
        }
    }, false);
};

window.mainLayoutSessionId = 0;
window.mainLayoutHotkeys = [];
window.mainLayoutSetHotkeys = (sessionId, hotkeys) => {
    window.mainLayoutSessionId = sessionId;
    window.mainLayoutHotkeys = hotkeys;
};

window.mainLayoutGetModifiers = () => {
    var res = 0;
    if (window.customShiftState) res += 1;
    if (window.customCtrlState) res += 2;
    return res;
};
