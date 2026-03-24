// YonetIQ — Dosya indirme yardımcı fonksiyonu
// Blazor Server'da dosya indirme için base64 verisi JS tarafında blob URL'ye dönüştürülür.
window.downloadFileFromBytes = function (fileName, base64, mimeType) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.printHtmlReport = function (htmlContent, title) {
    const win = window.open('', '_blank');
    win.document.write(`<!DOCTYPE html><html><head>
        <meta charset="utf-8">
        <title>${title}</title>
        <style>
            body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #1d273b; margin: 20px; }
            h1 { font-size: 16px; color: #E31E24; border-bottom: 2px solid #E31E24; padding-bottom: 6px; margin-bottom: 12px; }
            table { width: 100%; border-collapse: collapse; margin-top: 8px; }
            th { background: #E31E24; color: #fff; padding: 6px 8px; text-align: left; font-size: 11px; }
            td { padding: 5px 8px; border-bottom: 1px solid #e5e7eb; font-size: 11px; }
            tr:nth-child(even) td { background: #fff5f5; }
            .footer { margin-top: 20px; font-size: 10px; color: #888; text-align: right; }
            @media print { body { margin: 0; } }
        </style>
    </head><body>${htmlContent}</body></html>`);
    win.document.close();
    win.focus();
    setTimeout(() => { win.print(); }, 400);
};
