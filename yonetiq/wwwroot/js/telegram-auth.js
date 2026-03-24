/**
 * YonetIQ — Telegram Login Widget Helper
 * Telegram'ın resmi widget script'ini dinamik olarak hedef container'a enjekte eder.
 * Kullanıcı Telegram ile oturum açtığında Blazor .NET metoduna callback gönderir.
 */

/**
 * @param {string} containerId   - Widget'ın ekleneceği <div> id'si
 * @param {string} botUsername   - Telegram Bot kullanıcı adı (@ olmadan)
 * @param {object} dotNetRef     - DotNetObjectReference (Blazor interop)
 */
window.initTelegramWidget = function (containerId, botUsername, dotNetRef) {
    var container = document.getElementById(containerId);
    if (!container) return;

    // Önceki içeriği temizle
    container.innerHTML = '';

    // Global auth callback — Telegram widget bunu çağırır
    window._tgAuthCallback = function (user) {
        var json = JSON.stringify(user);
        dotNetRef.invokeMethodAsync('OnTelegramCallback', json)
            .catch(function (err) { console.error('[TG] Callback hatası:', err); });
    };

    // Widget script tag'ini oluştur ve enjekte et
    var script = document.createElement('script');
    script.src = 'https://telegram.org/js/telegram-widget.js?22';
    script.setAttribute('data-telegram-login', botUsername);
    script.setAttribute('data-size', 'medium');
    script.setAttribute('data-onauth', 'window._tgAuthCallback(user)');
    script.setAttribute('data-request-access', 'write');
    script.async = true;
    container.appendChild(script);
};

/**
 * Mevcut DotNetObjectReference'ı serbest bırakır (component dispose sırasında).
 * @param {object} dotNetRef
 */
window.disposeDotNetRef = function (dotNetRef) {
    if (dotNetRef) {
        dotNetRef.dispose();
    }
};
