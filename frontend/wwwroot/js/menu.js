document.addEventListener("DOMContentLoaded", function () {

    const menuOpen =
        document.getElementById("menuOpen");

    const menuClose =
        document.getElementById("menuClose");

    const sideMenu =
        document.getElementById("sideMenu");

    const menuOverlay =
        document.getElementById("menuOverlay");


    function openMenu() {

        if (!sideMenu || !menuOverlay) {
            return;
        }

        sideMenu.classList.add("active");

        menuOverlay.classList.add("active");

        document.body.classList.add("menu-open");
    }


    function closeMenu() {

        if (!sideMenu || !menuOverlay) {
            return;
        }

        sideMenu.classList.remove("active");

        menuOverlay.classList.remove("active");

        document.body.classList.remove("menu-open");
    }


    if (menuOpen) {

        menuOpen.addEventListener(
            "click",
            openMenu
        );

    }


    if (menuClose) {

        menuClose.addEventListener(
            "click",
            closeMenu
        );

    }


    if (menuOverlay) {

        menuOverlay.addEventListener(
            "click",
            closeMenu
        );

    }


    document.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Escape") {

                closeMenu();

            }

        }
    );

});