/**
 * ═══════════════════════════════════════════════════════════════
 * MODERN ULTRA-SMOOTH SCROLL REVEAL & PROGRESS CONTROLLER
 * Z-Stay UI / UX Experience
 * ═══════════════════════════════════════════════════════════════
 */
(function () {
    'use strict';

    // Đánh dấu JS đã sẵn sàng để kích hoạt hiệu ứng CSS mượt mà
    document.documentElement.classList.add('js-reveal-ready');

    let revealObserver = null;

    function initUIComponents() {
        // 1. Tạo thanh tiến trình cuộn trang ở đỉnh (Scroll Progress Bar)
        if (!document.getElementById('scrollProgressBar')) {
            const bar = document.createElement('div');
            bar.id = 'scrollProgressBar';
            document.body.prepend(bar);
        }

        // 2. Tạo nút cuộn mượt lên đầu trang (Back to Top)
        if (!document.getElementById('btnBackToTop')) {
            const btn = document.createElement('button');
            btn.id = 'btnBackToTop';
            btn.setAttribute('type', 'button');
            btn.setAttribute('aria-label', 'Cuộn lên đầu trang');
            btn.setAttribute('title', 'Lên đầu trang');
            btn.innerHTML = '<i class="bi bi-arrow-up"></i>';
            btn.addEventListener('click', function () {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
            document.body.appendChild(btn);
        }
    }

    function setupAutoRevealElements() {
        // Tự động gán hiệu ứng cho các khối chính của trang nếu chưa có class reveal
        const autoTargets = [
            'section:not(.no-reveal):not(.reveal)',
            '.section-header:not(.reveal)',
            '.room-card:not(.reveal)',
            '.landlord-card:not(.reveal)',
            '.landlord-grid-card:not(.reveal)',
            '.province-card:not(.reveal)',
            '.guide-card:not(.reveal)',
            '.contact-info-card:not(.reveal)',
            '.contact-form-card:not(.reveal)',
            '.contact-sidebar-card:not(.reveal)'
        ];

        document.querySelectorAll(autoTargets.join(', ')).forEach(el => {
            if (!el.classList.contains('reveal') && !el.classList.contains('is-revealed')) {
                // Thẻ card thì dùng hiệu ứng bay nhẹ lên
                if (el.matches('.room-card, .landlord-card, .landlord-grid-card, .province-card, .guide-card, .contact-info-card')) {
                    el.classList.add('reveal', 'reveal-scale');
                } else if (el.matches('.section-header')) {
                    el.classList.add('reveal', 'reveal-up');
                } else {
                    el.classList.add('reveal');
                }
            }
        });

        // Tạo hiệu ứng gối sóng (stagger) cho các hàng chứa nhiều thẻ
        const parentGrids = document.querySelectorAll('.row:not(.no-stagger), .grid:not(.no-stagger)');
        parentGrids.forEach(grid => {
            const children = grid.querySelectorAll('.reveal');
            children.forEach((child, index) => {
                const delayIndex = (index % 6) + 1;
                child.classList.add(`reveal-delay-${delayIndex}`);
            });
        });
    }

    function observeElements() {
        const revealElements = document.querySelectorAll('.reveal:not(.is-revealed)');
        if (!revealElements.length) return;

        // Nếu trình duyệt hỗ trợ IntersectionObserver
        if ('IntersectionObserver' in window) {
            if (revealObserver) {
                revealObserver.disconnect();
            }

            revealObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('is-revealed');
                        observer.unobserve(entry.target);
                    }
                });
            }, {
                root: null,
                rootMargin: '0px 0px -40px 0px',
                threshold: 0.05
            });

            revealElements.forEach(el => {
                // Kiểm tra nếu phần tử đã nằm trong khung nhìn ban đầu thì hiển thị tức thì
                const rect = el.getBoundingClientRect();
                if (rect.top < window.innerHeight && rect.bottom > 0) {
                    el.classList.add('is-revealed');
                } else {
                    revealObserver.observe(el);
                }
            });
        } else {
            // Fallback nếu trình duyệt quá cũ
            revealElements.forEach(el => el.classList.add('is-revealed'));
        }
    }

    // ── Xử lý cuộn trang: cập nhật thanh tiến trình & nút Back to top ──
    let isTicking = false;
    function onScroll() {
        if (!isTicking) {
            window.requestAnimationFrame(() => {
                const winScroll = document.documentElement.scrollTop || document.body.scrollTop;
                const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
                const scrolled = height > 0 ? (winScroll / height) * 100 : 0;

                const progressBar = document.getElementById('scrollProgressBar');
                if (progressBar) {
                    progressBar.style.width = scrolled + '%';
                }

                const backToTop = document.getElementById('btnBackToTop');
                if (backToTop) {
                    if (winScroll > 320) {
                        backToTop.classList.add('show');
                    } else {
                        backToTop.classList.remove('show');
                    }
                }

                isTicking = false;
            });
            isTicking = true;
        }
    }

    // ── Khởi chạy khi DOM sẵn sàng ──
    function init() {
        initUIComponents();
        setupAutoRevealElements();
        observeElements();
        window.addEventListener('scroll', onScroll, { passive: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Export hàm toàn cục để kích hoạt lại khi có AJAX nạp thêm phòng hoặc chuyển tab
    window.initScrollReveal = function () {
        setupAutoRevealElements();
        observeElements();
    };
})();
