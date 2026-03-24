/**
 * YonetIQ Theme Engine
 * Manages light/dark mode persistence and application.
 */

window.yiTheme = {
    getTheme: function () {
        return localStorage.getItem('yi-theme') || 'light';
    },
    setTheme: function (theme) {
        localStorage.setItem('yi-theme', theme);
        this.applyTheme(theme);
    },
    applyTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
    },
    init: function () {
        const theme = this.getTheme();
        this.applyTheme(theme);
    }
};

// Immediate execution to prevent FOUC
(function() {
    const theme = localStorage.getItem('yi-theme') || 'light';
    document.documentElement.setAttribute('data-theme', theme);
})();

// Chat container scroll helper
window.scrollElementToBottom = function (elementId) {
    const el = document.getElementById(elementId);
    if (el) el.scrollTop = el.scrollHeight;
};
