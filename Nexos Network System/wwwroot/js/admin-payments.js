(function () {
  // Receipt number copy
  document.addEventListener('click', function (e) {
    var btn = e.target.closest('.pay-copy');
    if (!btn) return;
    var text = btn.getAttribute('data-copy');
    var done = function () {
      btn.classList.add('is-done');
      setTimeout(function () { btn.classList.remove('is-done'); }, 1200);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(done);
    } else {
      var t = document.createElement('textarea');
      t.value = text; document.body.appendChild(t); t.select();
      document.execCommand('copy'); document.body.removeChild(t); done();
    }
  });

  // Export (current page rows) as CSV
  var exportBtn = document.getElementById('pay-export');
  var table = document.getElementById('pay-table');
  if (exportBtn && table) {
    exportBtn.addEventListener('click', function () {
      var clean = function (s) { return '"' + s.replace(/\s+/g, ' ').trim().replace(/"/g, '""') + '"'; };
      var rows = [];
      table.querySelectorAll('tr').forEach(function (tr) {
        var cells = Array.prototype.slice.call(tr.children, 0, -1); // Actions column skip
        rows.push(cells.map(function (c) { return clean(c.innerText); }).join(','));
      });
      var blob = new Blob(['\ufeff' + rows.join('\n')], { type: 'text/csv;charset=utf-8;' });
      var a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = 'payments-' + new Date().toISOString().slice(0, 10) + '.csv';
      document.body.appendChild(a); a.click(); document.body.removeChild(a);
      URL.revokeObjectURL(a.href);
    });
  }
})();
