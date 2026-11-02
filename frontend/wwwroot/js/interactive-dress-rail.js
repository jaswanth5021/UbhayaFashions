(() => {
    const section = document.querySelector("[data-dress-rail-section]");
    if (!section) return;

    const track = section.querySelector("[data-dress-rail-track]");
    const items = Array.from(section.querySelectorAll("[data-dress-rail-item]"));
    const dataNode = section.querySelector("[data-dress-rail-data]");
    const featureImage = section.querySelector("[data-dress-rail-feature-image]");
    const featureName = section.querySelector("[data-dress-rail-feature-name]");
    const featurePrice = section.querySelector("[data-dress-rail-feature-price]");
    const featureLink = section.querySelector("[data-dress-rail-feature-link]");
    if (!track || !items.length || !dataNode) return;

    let products;
    try { products = JSON.parse(dataNode.textContent); }
    catch { return; }

    let selectedIndex = 0;
    const selectItem = (index, scrollIntoView = false) => {
        const product = products[index];
        if (!product) return;
        selectedIndex = index;

        items.forEach((item, itemIndex) => {
            const selected = itemIndex === index;
            item.classList.toggle("is-selected", selected);
            item.setAttribute("aria-pressed", selected ? "true" : "false");
        });

        if (featureName) featureName.textContent = product.name || "";
        if (featurePrice) featurePrice.textContent = product.price || "";
        if (featureLink) featureLink.href = product.url || "#";

        if (featureImage && product.image) {
            featureImage.style.opacity = "0";
            window.setTimeout(() => {
                featureImage.src = product.image;
                featureImage.alt = product.name || "";
                featureImage.style.opacity = "1";
            }, 120);
        }

        if (scrollIntoView) {
            items[index].scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
        }
    };

    items.forEach((item, index) => {
        item.addEventListener("click", () => selectItem(index, false));
        // Hovering lifts the garment and previews its details.
        item.addEventListener("pointerenter", event => {
            if (event.pointerType === "mouse" || event.pointerType === "pen") {
                selectItem(index, false);
            }
        });
    });

    // A gentle cursor-follow interaction makes the garments travel along the rail.
    // Keep the pointer away from the extreme edges to avoid abrupt jumps.
    let hoverFrame = 0;
    track.addEventListener("pointermove", event => {
        if (event.pointerType !== "mouse" || pointerDown || track.scrollWidth <= track.clientWidth) return;
        if (hoverFrame) cancelAnimationFrame(hoverFrame);
        const bounds = track.getBoundingClientRect();
        const progress = Math.max(0, Math.min(1, (event.clientX - bounds.left) / bounds.width));
        const target = progress * (track.scrollWidth - track.clientWidth);
        hoverFrame = requestAnimationFrame(() => {
            track.scrollLeft += (target - track.scrollLeft) * 0.16;
        });
    });

    // Drag the rail with a mouse; native touch scrolling remains enabled on mobile.
    let pointerDown = false;
    let dragged = false;
    let startX = 0;
    let startScroll = 0;
    track.addEventListener("pointerdown", event => {
        if (event.pointerType === "touch" || event.button !== 0) return;
        pointerDown = true;
        dragged = false;
        startX = event.clientX;
        startScroll = track.scrollLeft;
    });
    track.addEventListener("pointermove", event => {
        if (!pointerDown) return;
        const delta = event.clientX - startX;
        if (Math.abs(delta) > 5) {
            dragged = true;
            track.classList.add("is-dragging");
            track.scrollLeft = startScroll - delta;
        }
    });
    const stopDrag = () => {
        pointerDown = false;
        track.classList.remove("is-dragging");
        if (dragged) {
            const centers = items.map(item => Math.abs(
                item.getBoundingClientRect().left + item.offsetWidth / 2 -
                (track.getBoundingClientRect().left + track.clientWidth / 2)
            ));
            const nearest = centers.indexOf(Math.min(...centers));
            if (nearest >= 0) selectItem(nearest, false);
        }
    };
    track.addEventListener("pointerup", stopDrag);
    track.addEventListener("pointercancel", stopDrag);
    track.addEventListener("mouseleave", stopDrag);
    track.addEventListener("click", event => {
        if (!dragged) return;
        event.preventDefault();
        event.stopPropagation();
        dragged = false;
    }, true);

    track.addEventListener("keydown", event => {
        if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
        event.preventDefault();
        const direction = event.key === "ArrowRight" ? 1 : -1;
        const nextIndex = Math.max(0, Math.min(items.length - 1, selectedIndex + direction));
        selectItem(nextIndex, true);
        items[nextIndex].focus();
    });
})();
