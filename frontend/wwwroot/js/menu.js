document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".desktop-nav-dropdown").forEach(function (dropdown) {
        if (!window.matchMedia("(min-width: 901px)").matches) return;

        dropdown.addEventListener("pointerenter", function () {
            dropdown.open = true;
        });

        dropdown.addEventListener("pointerleave", function () {
            dropdown.open = false;
        });
    });

    const menuOpen =
        document.getElementById("menuOpen");

    const menuClose =
        document.getElementById("menuClose");

    const sideMenu =
        document.getElementById("sideMenu");

    const menuOverlay =
        document.getElementById("menuOverlay");

    const sideMenuSearch =
        document.getElementById("sideMenuSearch");

    const headerSearchOpen =
        document.getElementById("headerSearchOpen");


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

    if (sideMenuSearch && headerSearchOpen) {
        sideMenuSearch.addEventListener("click", function () {
            closeMenu();
            window.setTimeout(function () {
                headerSearchOpen.click();
            }, 180);
        });
    }


    document.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Escape") {

                closeMenu();

            }

        }
    );


    // Desktop navigation sticks only after its original position is passed.
    // Hide it while scrolling down and reveal it again when scrolling up.
    const headerNavBar = document.querySelector(".header .header-nav-bar");
    const desktopNavQuery = window.matchMedia("(min-width: 901px)");
    if (headerNavBar && desktopNavQuery.matches) {
        let navStartY = headerNavBar.getBoundingClientRect().top + window.scrollY;
        let previousScrollY = window.scrollY;
        let scrollTicking = false;

        const updateScrollNavigation = function () {
            const currentY = window.scrollY;
            if (!desktopNavQuery.matches) {
                headerNavBar.classList.remove("is-scroll-fixed", "is-scroll-hidden");
                previousScrollY = currentY;
                scrollTicking = false;
                return;
            }

            if (currentY <= navStartY) {
                headerNavBar.classList.remove("is-scroll-fixed", "is-scroll-hidden");
            } else {
                headerNavBar.classList.add("is-scroll-fixed");
                if (currentY > previousScrollY + 2) {
                    headerNavBar.classList.add("is-scroll-hidden");
                } else if (currentY < previousScrollY - 2) {
                    headerNavBar.classList.remove("is-scroll-hidden");
                }
            }

            previousScrollY = currentY;
            scrollTicking = false;
        };

        window.addEventListener("scroll", function () {
            if (!scrollTicking) {
                scrollTicking = true;
                window.requestAnimationFrame(updateScrollNavigation);
            }
        }, { passive: true });

        window.addEventListener("resize", function () {
            if (!desktopNavQuery.matches) {
                headerNavBar.classList.remove("is-scroll-fixed", "is-scroll-hidden");
            } else if (!headerNavBar.classList.contains("is-scroll-fixed")) {
                navStartY = headerNavBar.getBoundingClientRect().top + window.scrollY;
            }
        }, { passive: true });
    }

});
