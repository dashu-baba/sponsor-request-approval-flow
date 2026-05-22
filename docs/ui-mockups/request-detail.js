(function () {
  var REQUESTS = {
    'SR-001': {
      title: 'TechConf 2025 Sponsorship', requestor: 'Sarah Chen', dept: 'Engineering', type: 'Conference',
      org: 'TechConf Asia', date: '2025-08-15', amount: 5000, status: 'Pending Manager Approval',
      submitted: '1 Jun 2025', purpose: 'Brand visibility at regional tech conference with 3,000+ attendees.',
      benefit: 'Lead generation and brand awareness in APAC developer community.',
      history: [{ action: 'Submitted: Draft → Pending Manager Approval', meta: 'Sarah Chen · Requestor · 1 Jun 2025 09:14' }]
    },
    'SR-002': {
      title: 'StartupBD Summit', requestor: 'Sarah Chen', dept: 'Business Dev', type: 'Summit',
      org: 'BD Alliance', date: '2025-09-20', amount: 12000, status: 'Pending Finance Review',
      submitted: '20 May 2025', purpose: 'Sponsor regional startup summit to establish BD partnerships.',
      benefit: 'Access to 200+ startup founders and investors.',
      history: [
        { action: 'Submitted: Draft → Pending Manager Approval', meta: 'Sarah Chen · Requestor · 20 May 2025 11:02' },
        { action: 'Approved: Pending Manager Approval → Pending Finance Review', meta: 'James Okafor · Manager · 21 May 2025 14:30', success: true, remarks: 'Strong BD alignment — proceed to finance.' }
      ]
    }
  };

  var ROLE_CFG = {
    requestor: { home: 'dashboard.html', dashLabel: 'My Requests', name: 'Sarah Chen', initials: 'SC', roleLabel: 'Requestor' },
    manager:   { home: 'dashboard-manager.html', dashLabel: 'Dashboard', name: 'James Okafor', initials: 'JO', roleLabel: 'Manager' },
    finance:   { home: 'dashboard-finance.html', dashLabel: 'Dashboard', name: 'Priya Mehta', initials: 'PM', roleLabel: 'Finance Admin' },
    admin:     { home: 'dashboard-admin.html', dashLabel: 'Dashboard', name: 'Alex Rivera', initials: 'AR', roleLabel: 'System Admin' }
  };

  var STATUS_BADGE = {
    'Pending Manager Approval': { cls: 'badge-pending-mgr', label: 'Pending Manager' },
    'Pending Finance Review': { cls: 'badge-pending-fin', label: 'Pending Finance' },
    'Approved': { cls: 'badge-approved', label: 'Approved' },
    'Rejected': { cls: 'badge-rejected', label: 'Rejected' },
    'Draft': { cls: 'badge-draft', label: 'Draft' },
    'Cancelled': { cls: 'badge-cancelled', label: 'Cancelled' }
  };

  var params = new URLSearchParams(window.location.search);
  var role = params.get('role') || 'requestor';
  var reqId = params.get('id') || 'SR-001';
  var cfg = ROLE_CFG[role] || ROLE_CFG.requestor;
  var req = REQUESTS[reqId] || REQUESTS['SR-001'];

  var files = [
    { id: '1', name: 'TechConf2025-Proposal.pdf', size: 253952, uploaded: '1 Jun 2025', ext: 'PDF' },
    { id: '2', name: 'Budget-Estimate.pdf', size: 91136, uploaded: '1 Jun 2025', ext: 'PDF' }
  ];
  var nextId = 3;
  var MAX_BYTES = 10 * 1024 * 1024;
  var ALLOWED_EXT = ['pdf', 'doc', 'docx', 'png', 'jpg', 'jpeg'];

  function fmtAmt(n) { return 'RM ' + Number(n).toLocaleString('en-MY'); }
  function fmtDate(iso) {
    if (!iso) return '—';
    var d = new Date(iso + 'T00:00:00');
    return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  function showToast(msg) {
    var t = document.getElementById('toast');
    document.getElementById('toast-text').textContent = msg;
    t.classList.add('show');
    setTimeout(function () { t.classList.remove('show'); }, 2800);
  }

  function openModal(id) { document.getElementById(id).classList.add('open'); }
  function closeModal(id) { document.getElementById(id).classList.remove('open'); }

  function canApprove() {
    return (role === 'manager' && req.status === 'Pending Manager Approval') ||
           (role === 'finance' && req.status === 'Pending Finance Review');
  }

  function setupChrome() {
    document.getElementById('brand-link').href = cfg.home;
    document.getElementById('back-link').href = cfg.home;
    document.getElementById('back-link').textContent = cfg.dashLabel;
    document.getElementById('back-btn').href = cfg.home;
    document.getElementById('nav-dashboard').href = cfg.home;
    document.getElementById('user-avatar').textContent = cfg.initials;
    document.getElementById('user-name').textContent = cfg.name;
    document.getElementById('user-role').textContent = cfg.roleLabel;
  }

  function setupRequest() {
    document.getElementById('req-id-label').textContent = reqId;
    document.getElementById('req-title').textContent = req.title;
    document.getElementById('req-sub').textContent = reqId + ' · ' + (req.submitted ? 'Submitted ' + req.submitted : 'Draft');
    document.getElementById('val-requestor').textContent = req.requestor;
    document.getElementById('val-dept').textContent = req.dept;
    document.getElementById('val-type').textContent = req.type;
    document.getElementById('val-org').textContent = req.org;
    document.getElementById('val-date').textContent = fmtDate(req.date);
    document.getElementById('val-amount').textContent = fmtAmt(req.amount);
    document.getElementById('val-purpose').textContent = req.purpose;
    document.getElementById('val-benefit').textContent = req.benefit;

    var b = STATUS_BADGE[req.status] || STATUS_BADGE['Draft'];
    var badgeEl = document.getElementById('req-badge');
    badgeEl.className = 'badge ' + b.cls;
    while (badgeEl.firstChild) badgeEl.removeChild(badgeEl.firstChild);
    var dot = document.createElement('span');
    dot.className = 'badge-dot';
    badgeEl.appendChild(dot);
    badgeEl.appendChild(document.createTextNode(b.label));

    var timeline = document.getElementById('timeline');
    while (timeline.firstChild) timeline.removeChild(timeline.firstChild);
    (req.history || []).forEach(function (h) {
      var item = document.createElement('div');
      item.className = 'timeline-item';
      var dotEl = document.createElement('div');
      dotEl.className = 'timeline-dot' + (h.success ? ' success' : h.danger ? ' danger' : '');
      var body = document.createElement('div');
      body.className = 'timeline-body';
      var action = document.createElement('div');
      action.className = 'timeline-action';
      action.textContent = h.action;
      var meta = document.createElement('div');
      meta.className = 'timeline-meta';
      meta.textContent = h.meta;
      body.appendChild(action);
      body.appendChild(meta);
      if (h.remarks) {
        var rem = document.createElement('div');
        rem.className = 'timeline-remarks';
        rem.textContent = h.remarks;
        body.appendChild(rem);
      }
      item.appendChild(dotEl);
      item.appendChild(body);
      timeline.appendChild(item);
    });
  }

  function setupApprovalActions() {
    var bar = document.getElementById('approval-actions');
    if (!canApprove()) {
      bar.hidden = true;
      return;
    }
    bar.hidden = false;
    document.getElementById('btn-approve').onclick = function () { openModal('modal-approve'); };
    document.getElementById('btn-reject').onclick = function () { openModal('modal-reject'); };
    document.getElementById('confirm-approve').onclick = function () {
      closeModal('modal-approve');
      showToast('Request approved successfully.');
      setTimeout(function () { window.location.href = cfg.home; }, 1200);
    };
    document.getElementById('confirm-reject').onclick = function () {
      var remarks = document.getElementById('reject-remarks').value.trim();
      if (!remarks) { showToast('Remarks are required when rejecting.'); return; }
      closeModal('modal-reject');
      showToast('Request rejected.');
      setTimeout(function () { window.location.href = cfg.home; }, 1200);
    };
  }

  function setupAttachments() {
    var uploadZone = document.getElementById('upload-zone');
    var fileInput = document.getElementById('file-input');
    var listEl = document.getElementById('attach-list');
    var emptyEl = document.getElementById('attach-empty');
    var isRequestor = role === 'requestor';

    if (!isRequestor) {
      uploadZone.hidden = true;
      document.getElementById('attach-hint').textContent = 'Read-only · uploaded by requestor';
    }

    function formatSize(bytes) {
      if (bytes < 1024) return bytes + ' B';
      if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + ' KB';
      return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function extLabel(name) {
      var parts = name.split('.');
      return parts.length > 1 ? parts.pop().toUpperCase().slice(0, 4) : 'FILE';
    }

    function isAllowed(file) {
      var parts = file.name.split('.');
      var ext = parts.length > 1 ? parts.pop().toLowerCase() : '';
      return ALLOWED_EXT.indexOf(ext) !== -1;
    }

    function renderAttachments() {
      while (listEl.firstChild) listEl.removeChild(listEl.firstChild);
      emptyEl.hidden = files.length > 0;
      files.forEach(function (file) {
        var item = document.createElement('div');
        item.className = 'attach-item';
        var icon = document.createElement('div');
        icon.className = 'attach-icon';
        icon.textContent = file.ext || extLabel(file.name);
        var info = document.createElement('div');
        info.className = 'attach-info';
        var name = document.createElement('div');
        name.className = 'attach-name';
        name.textContent = file.name;
        var meta = document.createElement('div');
        meta.className = 'attach-meta';
        meta.textContent = formatSize(file.size) + ' · Uploaded ' + file.uploaded;
        info.appendChild(name);
        info.appendChild(meta);
        item.appendChild(icon);
        item.appendChild(info);
        if (isRequestor) {
          var del = document.createElement('button');
          del.type = 'button';
          del.className = 'btn btn-sm attach-delete';
          del.textContent = 'Delete';
          del.addEventListener('click', function () {
            files = files.filter(function (f) { return f.id !== file.id; });
            renderAttachments();
            showToast('File removed.');
          });
          item.appendChild(del);
        }
        listEl.appendChild(item);
      });
    }

    function addFiles(fileList) {
      Array.prototype.forEach.call(fileList, function (file) {
        if (!isAllowed(file)) { showToast('File type not allowed: ' + file.name); return; }
        if (file.size > MAX_BYTES) { showToast('File exceeds 10 MB limit: ' + file.name); return; }
        files.push({ id: String(nextId++), name: file.name, size: file.size, uploaded: '22 May 2026', ext: extLabel(file.name) });
      });
      renderAttachments();
      if (fileList.length) showToast('File(s) uploaded.');
    }

    if (isRequestor) {
      fileInput.addEventListener('change', function () {
        if (fileInput.files && fileInput.files.length) addFiles(fileInput.files);
        fileInput.value = '';
      });
      uploadZone.addEventListener('dragover', function (e) { e.preventDefault(); uploadZone.classList.add('dragover'); });
      uploadZone.addEventListener('dragleave', function () { uploadZone.classList.remove('dragover'); });
      uploadZone.addEventListener('drop', function (e) {
        e.preventDefault();
        uploadZone.classList.remove('dragover');
        if (e.dataTransfer && e.dataTransfer.files.length) addFiles(e.dataTransfer.files);
      });
    }

    renderAttachments();
  }

  document.querySelectorAll('[data-close]').forEach(function (btn) {
    btn.addEventListener('click', function () { closeModal(btn.getAttribute('data-close')); });
  });
  document.querySelectorAll('.modal-overlay').forEach(function (m) {
    m.addEventListener('click', function (e) { if (e.target === m) m.classList.remove('open'); });
  });

  setupChrome();
  setupRequest();
  setupApprovalActions();
  setupAttachments();
})();
