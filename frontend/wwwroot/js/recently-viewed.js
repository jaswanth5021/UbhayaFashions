(() => {
    const section = document.querySelector("[data-recently-viewed]");
    if (!section) return;

    const list = section.querySelector("[data-recently-viewed-list]");
    const previousButton = section.querySelector("[data-recent-prev]");
    const nextButton = section.querySelector("[data-recent-next]");
    const storageKey = "ubhaya.recentlyViewed.v1";
    const currentId = String(section.dataset.productId || "");
    const maxItems = 6;

    const updateScrollButtons = () => {
        if (!list || !previousButton || !nextButton) return;
        const maxScroll = Math.max(0, list.scrollWidth - list.clientWidth);
        previousButton.disabled = list.scrollLeft <= 1;
        nextButton.disabled = list.scrollLeft >= maxScroll - 1;
    };

    const scrollHistory = direction => {
        const card = list?.querySelector(".recently-viewed-card");
        const gap = list ? parseFloat(getComputedStyle(list).columnGap) || 0 : 0;
        const distance = card ? (card.getBoundingClientRect().width + gap) * 2 : (list?.clientWidth || 0) * .8;
        list?.scrollBy({ left: direction * distance, behavior: "smooth" });
    };

    previousButton?.addEventListener("click", () => scrollHistory(-1));
    nextButton?.addEventListener("click", () => scrollHistory(1));
    list?.addEventListener("scroll", updateScrollButtons, { passive: true });
    window.addEventListener("resize", updateScrollButtons);

    const readHistory = () => {
        try {
            const parsed = JSON.parse(window.localStorage.getItem(storageKey) || "[]");
            return Array.isArray(parsed) ? parsed.filter(item => item && item.id && item.name) : [];
        } catch {
            return [];
        }
    };

    const current = {
        id: currentId,
        name: section.dataset.productName || "",
        category: section.dataset.productCategory || "",
        image: section.dataset.productImage || "",
        price: Number(section.dataset.productPrice) || 0,
        discount: Number(section.dataset.productDiscount) || 0,
        averageRating: Number(section.dataset.productRating) || 0,
        reviewCount: Number(section.dataset.productReviewCount) || 0,
        url: section.dataset.productUrl || "/Products/Details"
    };

    let localHistory = readHistory().filter(item => String(item.id) !== currentId);
    localHistory = [current, ...localHistory].slice(0, maxItems + 1);
    try {
        window.localStorage.setItem(storageKey, JSON.stringify(localHistory));
    } catch {
        // Keep the product detail page usable when storage is disabled or full.
    }

    const serverHistoryEnabled = section.dataset.serverHistoryEnabled === "true";
    let previousItems;
    if (serverHistoryEnabled) {
        try {
            const serverHistory = JSON.parse(section.dataset.serverHistory || "[]");
            previousItems = Array.isArray(serverHistory)
                ? serverHistory.filter(item => item && String(item.id) !== currentId).slice(0, maxItems)
                : [];
        } catch {
            previousItems = [];
        }
    } else {
        previousItems = localHistory.filter(item => String(item.id) !== currentId).slice(0, maxItems);
    }

    if (!list) return;

    const makeCard = item => {
        const product = {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name ?? "",
            category: item.category ?? item.Category ?? "",
            image: item.image ?? item.imageUrl ?? item.ImageUrl ?? "",
            price: Number(item.price ?? item.Price) || 0,
            discount: Number(item.discount ?? item.Discount) || 0,
            averageRating: Number(item.averageRating ?? item.AverageRating) || 0,
            reviewCount: Number(item.reviewCount ?? item.ReviewCount) || 0,
        };
        const article = document.createElement("article");
        article.className = "recently-viewed-card";

        const link = document.createElement("a");
        link.className = "recently-viewed-link";
        link.href = `/Products/Details/${encodeURIComponent(product.id)}`;

        const imageWrap = document.createElement("div");
        imageWrap.className = "recently-viewed-image";
        const fallback = document.createElement("div");
        fallback.className = "recently-viewed-fallback";
        fallback.textContent = "UBHAYA";
        fallback.hidden = Boolean(product.image);

        if (product.image) {
            const image = document.createElement("img");
            let imageSource = product.image;
            try {
                const resolvedImage = new URL(product.image, section.dataset.imageBase || window.location.origin);
                const isUploadedImage = resolvedImage.pathname.toLowerCase().startsWith("/uploads/");
                imageSource = isUploadedImage
                    ? new URL(`${resolvedImage.pathname}${resolvedImage.search}${resolvedImage.hash}`, window.location.origin).href
                    : resolvedImage.href;
            } catch { }
            image.alt = product.name;
            image.loading = "lazy";
            image.addEventListener("error", () => {
                image.hidden = true;
                fallback.hidden = false;
            }, { once: true });
            imageWrap.append(image);
            image.src = imageSource;
        }

        imageWrap.append(fallback);

        const rating = document.createElement("div");
        rating.className = "recently-viewed-rating";
        rating.hidden = product.reviewCount < 1;
        rating.setAttribute("aria-label", `${product.averageRating.toFixed(1)} out of 5 stars from ${product.reviewCount} reviews`);
        const ratingValue = document.createElement("span");
        ratingValue.className = "recently-viewed-rating-value";
        ratingValue.textContent = product.averageRating.toFixed(1);
        const ratingStar = document.createElement("span");
        ratingStar.className = "recently-viewed-rating-star";
        ratingStar.setAttribute("aria-hidden", "true");
        ratingStar.textContent = "★";
        const ratingDivider = document.createElement("span");
        ratingDivider.setAttribute("aria-hidden", "true");
        ratingDivider.textContent = "|";
        const ratingCount = document.createElement("span");
        ratingCount.className = "recently-viewed-rating-count";
        ratingCount.textContent = String(product.reviewCount);
        rating.append(ratingValue, ratingStar, ratingDivider, ratingCount);
        imageWrap.append(rating);

        if (product.discount > 0 && product.discount < 100) {
            const badge = document.createElement("span");
            badge.className = "recently-viewed-badge";
            badge.textContent = `-${product.discount}% OFF`;
            imageWrap.append(badge);
        }

        const category = document.createElement("span");
        category.className = "recently-viewed-category";
        category.textContent = product.category;
        const title = document.createElement("h3");
        title.className = "recently-viewed-name";
        title.textContent = product.name;
        const pricing = document.createElement("div");
        pricing.className = "recently-viewed-pricing";
        const price = document.createElement("span");
        price.className = "recently-viewed-price";
        price.textContent = `\u20B9${product.price.toLocaleString("en-IN", { maximumFractionDigits: 0 })}`;
        pricing.append(price);

        if (product.discount > 0 && product.discount < 100) {
            const original = document.createElement("span");
            original.className = "recently-viewed-original-price";
            const originalPrice = product.price / (1 - product.discount / 100);
            original.textContent = `\u20B9${Math.round(originalPrice).toLocaleString("en-IN")}`;
            pricing.append(original);
        }

        link.append(imageWrap, category, title, pricing);
        article.append(link);

        if (product.reviewCount < 1 && section.dataset.reviewSummaryUrl) {
            const summaryUrl = new URL(section.dataset.reviewSummaryUrl, window.location.origin);
            summaryUrl.searchParams.set("productId", String(product.id));
            fetch(summaryUrl, { credentials: "same-origin" })
                .then(response => response.ok ? response.json() : null)
                .then(summary => {
                    const count = Number(summary?.reviewCount) || 0;
                    if (!count) return;
                    const average = Number(summary.averageRating) || 0;
                    ratingValue.textContent = average.toFixed(1);
                    ratingCount.textContent = String(count);
                    rating.setAttribute("aria-label", `${average.toFixed(1)} out of 5 stars from ${count} reviews`);
                    rating.hidden = false;
                    product.averageRating = average;
                    product.reviewCount = count;
                })
                .catch(() => {});
        }
        return article;
    };

    const renderItems = items => {
        const visibleItems = items
            .filter(item => item && String(item.id ?? item.Id) !== currentId)
            .slice(0, maxItems);
        list.replaceChildren(...visibleItems.map(makeCard));
        section.hidden = visibleItems.length === 0;
        window.requestAnimationFrame(updateScrollButtons);
    };

    renderItems(previousItems);

    if (serverHistoryEnabled && localHistory.length && section.dataset.historySyncUrl) {
        const token = section.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            const body = new URLSearchParams({ __RequestVerificationToken: token });
            localHistory.forEach(item => body.append("productIds", String(item.id)));
            fetch(section.dataset.historySyncUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
                body
            }).then(response => response.ok ? response.json() : null)
                .then(items => {
                    if (Array.isArray(items)) renderItems(items);
                })
                .catch(() => {});
        }
    }
})();
