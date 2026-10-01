document.addEventListener('DOMContentLoaded', function () {
    var sidebarToggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('sidebar');

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });

        document.addEventListener('click', function (e) {
            if (sidebar.classList.contains('open') &&
                !sidebar.contains(e.target) &&
                e.target !== sidebarToggle &&
                !sidebarToggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }

    var navBadge = document.getElementById('navTicketCount');
    if (navBadge) {
        fetch('/Tickets/Count')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data && data.count > 0) {
                    navBadge.textContent = data.count;
                    navBadge.style.display = 'inline-flex';
                }
            })
            .catch(function () { });
    }

    var notificationBadge = document.getElementById('navNotificationCount');
    if (notificationBadge) {
        fetch('/Notifications/Count')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data && data.count > 0) {
                    notificationBadge.textContent = data.count;
                    notificationBadge.style.display = 'inline-flex';
                }
            })
            .catch(function () { });
    }

    initProductSelects();
});

function initProductSelects() {
    var selects = document.querySelectorAll('[data-product-select]');

    selects.forEach(function (select) {
        var trigger = select.querySelector('.product-select-trigger');
        var menu = select.querySelector('.product-select-menu');
        var hidden = select.querySelector('input[type="hidden"]');
        var label = trigger.querySelector('.product-select-label');
        var image = trigger.querySelector('.product-select-img');
        var options = Array.prototype.slice.call(select.querySelectorAll('.product-select-option'));
        var value = hidden ? (hidden.value || '') : '';

        function setTrigger(option) {
            var match = option || options.filter(function (o) {
                return o.getAttribute('data-value') === value;
            })[0];

            options.forEach(function (o) {
                var isSelected = o === match;
                o.classList.toggle('selected', isSelected);
            });

            if (match) {
                label.textContent = match.getAttribute('data-label') || 'Not verified';
                image.src = match.getAttribute('data-image') || '';
            } else {
                label.textContent = 'Not verified';
                image.src = '';
            }
        }

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = menu.classList.toggle('open');
            trigger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });

        options.forEach(function (option) {
            option.addEventListener('click', function () {
                value = option.getAttribute('data-value') || '';
                if (hidden) { hidden.value = value; }
                setTrigger(option);
                menu.classList.remove('open');
                trigger.setAttribute('aria-expanded', 'false');
            });
        });

        document.addEventListener('click', function (e) {
            if (!select.contains(e.target)) {
                menu.classList.remove('open');
                trigger.setAttribute('aria-expanded', 'false');
            }
        });

        setTrigger(null);
    });
}
