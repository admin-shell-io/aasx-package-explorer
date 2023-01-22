// see: https://blog.ppedv.de/post/Blazor-Navbar-Top-Menu-auf-Bootstrap-Basis

// resharper disable all

// these functions are used by the MainLayout.razor
// to automate / integrate Bootstrap menues

function mainLayoutOpenDropDown() {

    // desparate debug measures ..
    //if (event.target.id == "navbarDropdown2")
    //    alert("Hallo on " + event.target.id);

    // this line was IN the original code
    mainLayoutCleanDropDown();
    var parent = this.parentNode;

    // this was added, but seems to have no effect
    if (parent && parent.parentNode) {
        // alert("Appraoching " + parent.parentNode.id);
        parent.parentNode.classList.toggle("show");
    }

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

window.mainLayoutAttachHandlers = () => {
    var elements = document.getElementsByClassName('dropdown-toggle');
    for (var i = 0; i < elements.length; i++) {
        // alert("Attach " + elements[i].id);
        elements[i].addEventListener("click", mainLayoutOpenDropDown, false);
    }
}

// some more code to influence the existing menu

window.setNavBarItem = (id, title) => {
    // alert("SetNavBarItem " + id + " = " + title);
    var anchor_by_id = document.getElementById(id);
    if (anchor_by_id) {
        anchor_by_id.innerText = "" + title;
    }
}