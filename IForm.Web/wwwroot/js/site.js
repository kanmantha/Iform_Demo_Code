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
});
