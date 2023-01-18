// resharper disable all

// these functions are used by the MainLayout.razor
// to automate / integrate Bootstrap menues

function mainLayoutOpenDropDown() {
    alert("Hallo");
    cleanDropDown();
    var parent = this.parentNode;
    parent.classList.toggle("show");
    parent.querySelector('.dropdown-menu').classList.toggle("show");
}

function mainLayoutCleanDropDown() {
    var dropdowns = document.getElementsByClassName("dropdown-menu");
    var i;
    for (i = 0; i < dropdowns.length; i++) {
        var openDropdown = dropdowns[i];
        if (openDropdown.classList.contains('show')) {
            openDropdown.classList.remove('show');
        }
    }
}

// the following will add a callback to the GENERAL BROWSER WINDOW !!

window.onclick = function (event) {
    if (!event.target.matches('.dropdown-toggle')) {
        mainLayoutCleanDropDown();
    }
}

window.attachHandlers = () => {
    var elements = document.getElementsByClassName('dropdown-toggle');
    for (var i = 0; i < elements.length; i++) {
        elements[i].addEventListener("click", openDropDown, false);
    }
}