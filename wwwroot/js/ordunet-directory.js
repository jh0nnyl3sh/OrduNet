// OrduNet Telefon Rehberi - Canlı Arama, Filtreleme ve Kopyalama Scripti
document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById("directorySearchInput");
    const clearBtn = document.getElementById("btnClearSearch");
    const categoryPills = document.querySelectorAll(".category-pill-btn");
    const tableBody = document.getElementById("directoryTableBody");
    const cardsContainer = document.getElementById("directoryCardsContainer");
    const resultCountBadge = document.getElementById("resultCountBadge");
    const noResultsDiv = document.getElementById("noResultsDiv");
    const copyToast = document.getElementById("copyToast");

    let currentCategory = "Tümü";
    let debounceTimer;

    // Arama kutusu değiştiğinde
    if (searchInput) {
        searchInput.addEventListener("input", function () {
            const query = this.value.trim();
            if (clearBtn) {
                clearBtn.style.display = query.length > 0 ? "block" : "none";
            }

            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                performSearch(query, currentCategory);
            }, 250);
        });

        // Enter'a basıldığında formu göndermesin
        searchInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
            }
        });
    }

    // Temizle butonu
    if (clearBtn) {
        clearBtn.addEventListener("click", function () {
            if (searchInput) {
                searchInput.value = "";
                this.style.display = "none";
                performSearch("", currentCategory);
                searchInput.focus();
            }
        });
    }

    // Kategori butonları
    categoryPills.forEach(pill => {
        pill.addEventListener("click", function (e) {
            e.preventDefault();
            categoryPills.forEach(p => p.classList.remove("active"));
            this.classList.add("active");
            currentCategory = this.getAttribute("data-category") || "Tümü";

            const query = searchInput ? searchInput.value.trim() : "";
            performSearch(query, currentCategory);
        });
    });

    // Canlı Arama Fonksiyonu
    function performSearch(query, category) {
        const url = `/Directory/Search?q=${encodeURIComponent(query)}&category=${encodeURIComponent(category)}`;

        fetch(url)
            .then(res => res.json())
            .then(res => {
                const data = res.data;
                const count = res.count;

                if (resultCountBadge) {
                    resultCountBadge.textContent = `${count} Kayıt Bulundu`;
                }

                if (count === 0) {
                    if (tableBody) tableBody.innerHTML = "";
                    if (cardsContainer) cardsContainer.innerHTML = "";
                    if (noResultsDiv) noResultsDiv.style.display = "block";
                    return;
                }

                if (noResultsDiv) noResultsDiv.style.display = "none";

                // Tabloyu Güncelle
                if (tableBody) {
                    renderTable(data);
                }

                // Kartları Güncelle (Varsa)
                if (cardsContainer) {
                    renderCards(data);
                }
            })
            .catch(err => {
                console.error("Rehber arama hatası:", err);
            });
    }

    // Tablo Satırlarını Oluştur
    function renderTable(list) {
        let html = "";
        list.forEach((p, idx) => {
            const secondInternal = p.internalNumber2 ? `<span class="text-muted ms-1">/ ${escapeHtml(p.internalNumber2)}</span>` : "";
            const emailHtml = p.email ? `<a href="mailto:${escapeHtml(p.email)}" class="text-decoration-none text-muted" title="${escapeHtml(p.email)}"><i class="bi bi-envelope"></i></a>` : "-";
            const roomHtml = p.roomNumber ? `<span class="room-badge">${escapeHtml(p.roomNumber)}</span>` : "-";
            const floorHtml = p.floor ? `<small class="text-muted d-block">${escapeHtml(p.floor)}</small>` : "";

            html += `
                <tr>
                    <td class="text-center text-muted" style="width: 50px;">${idx + 1}</td>
                    <td>
                        <div class="personnel-name">${escapeHtml(p.fullName)}</div>
                        <div class="personnel-title">${escapeHtml(p.title)}</div>
                    </td>
                    <td>
                        <div class="fw-semibold text-dark">${escapeHtml(p.unitName)}</div>
                        <small class="badge bg-light text-secondary border">${escapeHtml(p.category)}</small>
                    </td>
                    <td>
                        <span class="internal-badge copy-internal" data-number="${escapeHtml(p.internalNumber)}" title="Tıkla ve Dahiliyi Kopyala">
                            <i class="bi bi-telephone-fill"></i> ${escapeHtml(p.internalNumber)}
                        </span>
                        ${secondInternal}
                    </td>
                    <td>
                        ${roomHtml}
                        ${floorHtml}
                    </td>
                    <td class="text-center">
                        ${emailHtml}
                    </td>
                </tr>
            `;
        });
        tableBody.innerHTML = html;
        bindCopyButtons();
    }

    // Dahili Kopyalama Olayı
    function bindCopyButtons() {
        document.querySelectorAll(".copy-internal").forEach(badge => {
            badge.addEventListener("click", function () {
                const number = this.getAttribute("data-number");
                if (!number) return;

                navigator.clipboard.writeText(number).then(() => {
                    showCopyToast(`Dahili No: ${number} kopyalandı!`);
                }).catch(() => {
                    // Fallback
                    showCopyToast(`Dahili: ${number}`);
                });
            });
        });
    }

    function showCopyToast(msg) {
        if (!copyToast) return;
        copyToast.querySelector(".toast-message").textContent = msg;
        copyToast.classList.add("show");
        setTimeout(() => {
            copyToast.classList.remove("show");
        }, 2200);
    }

    function escapeHtml(str) {
        if (!str) return "";
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // İlk yüklemede kopyalama butonlarını bağla
    bindCopyButtons();
});
