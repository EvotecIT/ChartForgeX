(function () {
  const cards = document.querySelectorAll(".link-card");
  for (const card of cards) {
    card.addEventListener("mousemove", (event) => {
      const rect = card.getBoundingClientRect();
      card.style.setProperty("--x", `${event.clientX - rect.left}px`);
      card.style.setProperty("--y", `${event.clientY - rect.top}px`);
    });
  }

  const optionalPreviews = document.querySelectorAll("img[data-optional-preview]");
  for (const image of optionalPreviews) {
    const previewCard = image.closest("[data-preview-card]");
    const markMissing = () => {
      if (previewCard) previewCard.classList.add("is-missing-preview");
    };

    image.addEventListener("error", markMissing, { once: true });
    if (!image.getAttribute("src") || (image.complete && image.naturalWidth === 0)) {
      markMissing();
    }
  }

  for (const card of document.querySelectorAll("[data-example]")) {
    const pills = Array.from(card.querySelectorAll("[data-format]"));
    const frame = card.querySelector("[data-preview-frame]");
    const host = card.querySelector("[data-preview-host]");
    const call = card.querySelector("[data-preview-call]");
    const open = card.querySelector("[data-open]");
    if (!pills.length || !frame) continue;

    const urls = {
      svg: card.dataset.svg,
      png: card.dataset.png,
      html: card.dataset.html
    };
    const slug = (urls.svg || "").split("/").pop()?.replace(/\.svg$/i, "") || "chart";
    const calls = {
      svg: `chart.SaveSvg("${slug}.svg");`,
      png: `chart.SavePng("${slug}.png");`,
      html: `chart.SaveHtml("${slug}.html");`
    };

    const renderFormat = (format) => {
      const url = urls[format];
      if (!url) return;

      const existing = frame.querySelector(".preview-img, .preview-iframe");
      if (existing) existing.remove();

      if (format === "html") {
        const iframe = document.createElement("iframe");
        iframe.className = "preview-iframe";
        iframe.src = url;
        iframe.title = `${slug} HTML preview`;
        iframe.loading = "lazy";
        iframe.setAttribute("sandbox", "allow-same-origin allow-scripts");
        frame.appendChild(iframe);
      } else {
        const image = document.createElement("img");
        image.className = "preview-img";
        image.src = url;
        image.alt = `${slug} ${format.toUpperCase()} preview`;
        image.loading = "lazy";
        image.decoding = "async";
        frame.appendChild(image);
      }

      frame.dataset.current = format;
      if (host) host.textContent = `${slug}.${format}`;
      if (call) call.textContent = calls[format] || "";
      if (open) open.href = url;
    };

    for (const pill of pills) {
      pill.addEventListener("click", () => {
        const format = pill.dataset.format || "svg";
        for (const other of pills) {
          const active = other === pill;
          other.classList.toggle("is-active", active);
          other.setAttribute("aria-pressed", active ? "true" : "false");
        }
        renderFormat(format);
      });
    }
  }

  const copyText = async (text) => {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return true;
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "fixed";
    textarea.style.top = "-1000px";
    document.body.appendChild(textarea);
    textarea.select();
    const ok = document.execCommand("copy");
    textarea.remove();
    return ok;
  };

  for (const button of document.querySelectorAll("[data-copy-case]")) {
    button.addEventListener("click", async () => {
      const card = button.closest("[data-case-title]");
      if (!card) return;

      const text = [
        `Case: ${card.dataset.caseTitle || ""}`,
        `Source: ${card.dataset.caseSource || ""}`,
        `Entry: ${card.dataset.caseEntry || ""}`,
        `Output: ${card.dataset.caseOutput || ""}`,
        `Path: ${card.dataset.casePath || ""}`
      ].join("\n");

      try {
        await copyText(text);
        const previous = button.textContent;
        button.textContent = "Copied";
        window.setTimeout(() => {
          button.textContent = previous;
        }, 1800);
      } catch (_) {
        button.textContent = "Copy failed";
      }
    });
  }

  for (const button of document.querySelectorAll("[data-copy-artifacts]")) {
    button.addEventListener("click", async () => {
      const card = button.closest("[data-case-title]");
      if (!card) return;

      const urls = Array.from(card.querySelectorAll(".artifact-links a, .artifact-links [data-artifact-href]"))
        .map((link) => new URL(link.getAttribute("href") || link.dataset.artifactHref, window.location.origin).href)
        .join("\n");

      try {
        await copyText(urls);
        const previous = button.textContent;
        button.textContent = "Copied";
        window.setTimeout(() => {
          button.textContent = previous;
        }, 1800);
      } catch (_) {
        button.textContent = "Copy failed";
      }
    });
  }

  for (const button of document.querySelectorAll("[data-copy-markdown]")) {
    button.addEventListener("click", async () => {
      const card = button.closest("[data-case-title]");
      if (!card) return;

      const artifactLinks = Array.from(card.querySelectorAll(".artifact-links a, .artifact-links [data-artifact-href]"))
        .map((link) => `- [${link.textContent.trim()}](${link.getAttribute("href") || link.dataset.artifactHref})`)
        .join("\n");
      const sourceLink = card.dataset.caseSourceUrl || card.querySelector("[data-case-source-link]")?.getAttribute("href") || "";
      const title = card.dataset.caseTitle || "";
      const text = [
        `### ${title}`,
        "",
        card.querySelector(".case-body p")?.textContent.trim() || "",
        "",
        `Source: [${card.dataset.caseSource || ""}](${sourceLink})`,
        `Entry: \`${card.dataset.caseEntry || ""}\``,
        `Output path: \`${card.dataset.casePath || ""}\``,
        "",
        artifactLinks
      ].join("\n");

      try {
        await copyText(text);
        const previous = button.textContent;
        button.textContent = "Copied";
        window.setTimeout(() => {
          button.textContent = previous;
        }, 1800);
      } catch (_) {
        button.textContent = "Copy failed";
      }
    });
  }

  const caseCards = Array.from(document.querySelectorAll(".case-card[data-case-title]"));
  const searchInput = document.querySelector("[data-case-search]");
  const filterButtons = Array.from(document.querySelectorAll("[data-case-filter]"));
  const emptyState = document.querySelector("[data-case-empty]");
  const countLabel = document.querySelector("[data-case-count]");
  const resetButton = document.querySelector("[data-case-reset]");
  let activeFilter = "all";

  const applyCaseFilters = () => {
    const query = (searchInput?.value || "").trim().toLowerCase();
    let visible = 0;

    for (const card of caseCards) {
      const tags = `${card.dataset.caseTitle || ""} ${card.dataset.caseTags || ""} ${card.dataset.caseSource || ""} ${card.dataset.caseEntry || ""}`.toLowerCase();
      const matchesQuery = !query || tags.includes(query);
      const matchesFilter = activeFilter === "all" || tags.includes(activeFilter);
      const show = matchesQuery && matchesFilter;
      card.classList.toggle("is-hidden", !show);
      if (show) visible += 1;
    }

    if (emptyState) emptyState.hidden = visible > 0;
    if (countLabel) {
      const suffix = caseCards.length === 1 ? "case" : "cases";
      countLabel.textContent = `${visible} of ${caseCards.length} promoted ${suffix}`;
    }
  };

  if (searchInput) {
    searchInput.addEventListener("input", applyCaseFilters);
  }

  for (const button of filterButtons) {
    button.addEventListener("click", () => {
      activeFilter = button.dataset.caseFilter || "all";
      for (const other of filterButtons) {
        const active = other === button;
        other.classList.toggle("is-active", active);
        other.setAttribute("aria-pressed", active ? "true" : "false");
      }
      applyCaseFilters();
    });
  }

  if (resetButton) {
    resetButton.addEventListener("click", () => {
      activeFilter = "all";
      if (searchInput) searchInput.value = "";
      for (const button of filterButtons) {
        const active = button.dataset.caseFilter === "all";
        button.classList.toggle("is-active", active);
        button.setAttribute("aria-pressed", active ? "true" : "false");
      }
      applyCaseFilters();
    });
  }

  applyCaseFilters();

  const galleryButtons = Array.from(document.querySelectorAll("[data-gallery-filter]"));
  const galleryCards = Array.from(document.querySelectorAll("[data-gallery-card]"));
  for (const button of galleryButtons) {
    button.addEventListener("click", () => {
      const filter = button.dataset.galleryFilter || "all";
      for (const other of galleryButtons) {
        const active = other === button;
        other.classList.toggle("is-active", active);
        other.setAttribute("aria-pressed", active ? "true" : "false");
      }
      for (const card of galleryCards) {
        const show = filter === "all" || card.dataset.category === filter;
        card.classList.toggle("is-hidden", !show);
      }
    });
  }

  for (const pre of document.querySelectorAll(".content-panel pre, .ev-content-card pre, .ev-docs-card pre, .example-code")) {
    if (pre.querySelector(".copy-code-button")) continue;
    const code = pre.querySelector("code");
    if (!code) continue;

    pre.classList.add("has-copy-button");
    const button = document.createElement("button");
    button.type = "button";
    button.className = "copy-code-button";
    button.textContent = "Copy";
    button.addEventListener("click", async () => {
      try {
        await copyText(code.innerText.trim());
        button.textContent = "Copied";
        window.setTimeout(() => {
          button.textContent = "Copy";
        }, 1800);
      } catch (_) {
        button.textContent = "Copy failed";
      }
    });
    pre.prepend(button);
  }
})();
