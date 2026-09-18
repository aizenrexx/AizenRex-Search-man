// ==============================================================================
// AizenRex Search-man - Modern Web Frontend (No module dependencies, 100% robust)
// ==============================================================================

// IPC bridge to C# WebView2
window.ipc = {
  postMessage: (obj) => {
    if (window.chrome && window.chrome.webview) {
      try {
        window.chrome.webview.postMessage(JSON.stringify(obj));
      } catch {
        window.chrome.webview.postMessage(obj);
      }
    } else {
      console.log('IPC message:', obj);
    }
  }
};

// Listen to high-speed native WebMessages from C#
if (window.chrome && window.chrome.webview) {
  window.chrome.webview.addEventListener('message', (event) => {
    try {
      const msg = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
      if (msg && msg.type && typeof window[msg.type] === 'function') {
        window[msg.type](msg.data);
      }
    } catch (e) {
      console.error('IPC dispatch error:', e);
    }
  });
}

const $ = (id) => document.getElementById(id);

// ---------------------------------------------------------------------------
// Version reporting: the native host pushes the running version on navigation,
// so the window title, the badge and the About dialog are always in agreement.
// ---------------------------------------------------------------------------
window.appInfo = (info) => {
  const v = (info && info.version) ? String(info.version) : '';
  if (!v) return;

  document.title = `AizenRex Search-man v${v} Professional`;

  const brand = document.querySelector('.brand-logo-wrap');
  if (brand) brand.title = `AizenRex Search-man v${v} Professional`;

  const badge = $('buildVersion');
  if (badge) badge.textContent = `v${v} (.NET 9)`;

  const about = $('aboutVersion');
  if (about) about.textContent = `v${v} Professional Edition (.NET 9 + WebView2)`;
};


function svgDataUri(svg) {
  return 'data:image/svg+xml;base64,' + btoa(svg);
}

// Crisp Fluent/Windows 11 styled SVG icons for immediate 0ms rendering
const DEFAULT_ICONS = {
  __folder__: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#f8c440" d="M1 3.5C1 2.67 1.67 2 2.5 2h3l1.5 1.5h6.5c.83 0 1.5.67 1.5 1.5v7.5c0 .83-.67 1.5-1.5 1.5h-11C1.67 14 1 13.33 1 12.5z"/><path fill="#ffb020" d="M1 5.5h14L13.8 13c-.1.6-.6 1-1.2 1H2.4c-.6 0-1.1-.4-1.2-1z"/></svg>'),
  __file__: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#90a4ae" d="M2.5 1.5a1 1 0 0 1 1-1h5.5L13.5 5v9.5a1 1 0 0 1-1 1h-9a1 1 0 0 1-1-1z"/><path fill="#cfd8dc" d="M9 1.5V5h4.5z"/></svg>'),
  exe: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="14" height="14" x="1" y="1" rx="2" fill="#0078d4"/><rect width="10" height="2" x="3" y="3" fill="#ffffff" opacity=".8"/><polygon points="5,7 5,11 9,9" fill="#ffffff"/></svg>'),
  lnk: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="14" height="14" x="1" y="1" rx="2" fill="#0078d4"/><polygon points="5,7 5,11 9,9" fill="#ffffff"/><path fill="#ffffff" d="M10 10l3 3m0-3v3h-3"/></svg>'),
  pdf: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#ea3636" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="3" y="12" font-size="5" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">PDF</text></svg>'),
  doc: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#185abd" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">W</text></svg>'),
  docx: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#185abd" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">W</text></svg>'),
  xls: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#107c41" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">X</text></svg>'),
  xlsx: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#107c41" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">X</text></svg>'),
  csv: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#107c41" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">X</text></svg>'),
  ppt: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#c43e1c" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">P</text></svg>'),
  pptx: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#c43e1c" d="M2 2a1 1 0 0 1 1-1h6.5L14 5.5V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1z"/><path fill="#fff" opacity=".3" d="M9.5 1v4.5H14z"/><text x="4" y="12" font-size="6" font-family="Segoe UI,sans-serif" font-weight="bold" fill="#fff">P</text></svg>'),
  zip: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="12" height="13" x="2" y="1.5" rx="1.5" fill="#744da9"/><rect width="2" height="7" x="7" y="1.5" fill="#ffd700"/><rect width="4" height="3" x="6" y="8" rx="0.5" fill="#ffd700"/></svg>'),
  rar: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="12" height="13" x="2" y="1.5" rx="1.5" fill="#744da9"/><rect width="2" height="7" x="7" y="1.5" fill="#ffd700"/><rect width="4" height="3" x="6" y="8" rx="0.5" fill="#ffd700"/></svg>'),
  '7z': svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="12" height="13" x="2" y="1.5" rx="1.5" fill="#744da9"/><rect width="2" height="7" x="7" y="1.5" fill="#ffd700"/><rect width="4" height="3" x="6" y="8" rx="0.5" fill="#ffd700"/></svg>'),
  png: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#008272"/><circle cx="4.5" cy="5.5" r="1.5" fill="#fff"/><polygon points="2.5,12 6,8 8.5,10.5 11,7 13.5,12" fill="#fff" opacity=".8"/></svg>'),
  jpg: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#008272"/><circle cx="4.5" cy="5.5" r="1.5" fill="#fff"/><polygon points="2.5,12 6,8 8.5,10.5 11,7 13.5,12" fill="#fff" opacity=".8"/></svg>'),
  jpeg: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#008272"/><circle cx="4.5" cy="5.5" r="1.5" fill="#fff"/><polygon points="2.5,12 6,8 8.5,10.5 11,7 13.5,12" fill="#fff" opacity=".8"/></svg>'),
  webp: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#008272"/><circle cx="4.5" cy="5.5" r="1.5" fill="#fff"/><polygon points="2.5,12 6,8 8.5,10.5 11,7 13.5,12" fill="#fff" opacity=".8"/></svg>'),
  gif: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#008272"/><circle cx="4.5" cy="5.5" r="1.5" fill="#fff"/><polygon points="2.5,12 6,8 8.5,10.5 11,7 13.5,12" fill="#fff" opacity=".8"/></svg>'),
  mp3: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="13" x="1.5" y="1.5" rx="2" fill="#ec407a"/><path fill="#fff" d="M6 10a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm0-5v5h4V5l-4-1z"/></svg>'),
  wav: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="13" x="1.5" y="1.5" rx="2" fill="#ec407a"/><path fill="#fff" d="M6 10a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm0-5v5h4V5l-4-1z"/></svg>'),
  flac: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="13" x="1.5" y="1.5" rx="2" fill="#ec407a"/><path fill="#fff" d="M6 10a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm0-5v5h4V5l-4-1z"/></svg>'),
  mp4: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#5c2d91"/><polygon points="6,5 11,8 6,11" fill="#fff"/></svg>'),
  mkv: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#5c2d91"/><polygon points="6,5 11,8 6,11" fill="#fff"/></svg>'),
  avi: svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="13" height="11" x="1.5" y="2.5" rx="1" fill="#5c2d91"/><polygon points="6,5 11,8 6,11" fill="#fff"/></svg>')
};

// Category Badge Resolver for Cards & Previews
function getCategoryBadge(item) {
  if (item.IsFolder) return { label: 'Folder', cls: 'badge-folder' };
  const ext = (item.Ext || '').toLowerCase().replace(/^\./, '');
  if (['mp4', 'mkv', 'avi', 'mov', 'wmv', 'flv', 'webm', 'ts', 'm4v'].includes(ext)) {
    return { label: ext.toUpperCase() || 'VIDEO', cls: 'badge-video' };
  }
  if (['mp3', 'wav', 'flac', 'aac', 'ogg', 'm4a', 'wma', 'opus'].includes(ext)) {
    return { label: ext.toUpperCase() || 'AUDIO', cls: 'badge-audio' };
  }
  if (['pdf', 'doc', 'docx', 'txt', 'rtf', 'odt', 'xlsx', 'xls', 'csv', 'pptx', 'ppt'].includes(ext)) {
    return { label: ext.toUpperCase() || 'DOC', cls: 'badge-doc' };
  }
  if (['png', 'jpg', 'jpeg', 'webp', 'gif', 'bmp', 'svg', 'ico', 'tiff'].includes(ext)) {
    return { label: ext.toUpperCase() || 'IMG', cls: 'badge-image' };
  }
  if (['exe', 'msi', 'bat', 'cmd', 'ps1', 'lnk'].includes(ext)) {
    return { label: ext.toUpperCase() || 'APP', cls: 'badge-exe' };
  }
  if (['zip', 'rar', '7z', 'tar', 'gz', 'iso', 'bz2'].includes(ext)) {
    return { label: ext.toUpperCase() || 'ARCH', cls: 'badge-archive' };
  }
  return { label: (ext ? ext.toUpperCase() : 'FILE'), cls: 'badge-file' };
}

// ------------------------------------------------------------------------------
// VirtualGrid: Multi-Mode Virtualized Table & Card Grid with True Shell Icons
// Supports: Details List ('list'), Card / Tile Grid ('card'), Compact List ('compact')
// ------------------------------------------------------------------------------
class VirtualGrid {
  constructor(containerId, spacerId, visibleId, rowHeight = 24) {
    this.container = document.getElementById(containerId);
    this.spacer = document.getElementById(spacerId);
    this.visible = document.getElementById(visibleId);
    this.defaultRowHeight = rowHeight;
    this.rowHeight = rowHeight;
    this.items = [];
    this.selected = -1;
    this.selectedSet = new Set(); // multi-selection (feature: select_all / invert_selection)
    this.lastStart = -1;
    this.lastEnd = -1;
    this.lastSel = -1;
    this.totalMatches = 0;
    this.loadingMore = false;
    this.onLoadMore = null;
    this.rafId = null;
    this.isScrolling = false;
    this.scrollDebounceTimer = null;

    // View Mode: 'list' | 'card' | 'compact'
    this.viewMode = localStorage.getItem('aizen_view_mode') || 'list';
    this.cardWidth = 280;
    this.cardHeight = 94;
    this.cardGap = 10;
    this.cols = 1;

    // Cache of base64 shell icons & pre-populated default SVGs
    this.iconCache = Object.assign({}, DEFAULT_ICONS);
    this.iconPending = new Set();
    this.iconBusy = false;
    this.iconTimer = null;
    this.onSelect = null;
    this.onOpen = null;
    this.onContext = null;
    this.highlightQuery = '';

    this.container.addEventListener('scroll', () => {
      if (!this.isScrolling) {
        this.isScrolling = true;
        document.body.classList.add('is-scrolling');
      }
      clearTimeout(this.scrollDebounceTimer);
      this.scrollDebounceTimer = setTimeout(() => {
        this.isScrolling = false;
        document.body.classList.remove('is-scrolling');
      }, 120);

      this.queueRender();
    }, { passive: true });

    window.addEventListener('resize', () => {
      this.lastStart = -1;
      this.lastEnd = -1;
      this.updateViewMetrics();
      this.queueRender();
    });

    // Delegated event listeners for 60+ FPS zero-allocation scrolling & card actions
    this.visible.addEventListener('click', (e) => {
      const actBtn = e.target.closest('.card-act-btn');
      if (actBtn) {
        e.stopPropagation();
        const action = actBtn.dataset.action;
        const idx = parseInt(actBtn.dataset.idx, 10);
        const item = this.items[idx];
        if (!item) return;
        if (action === 'folder') window.ipc.postMessage({ action: 'folder', path: item.Path });
        if (action === 'copy') {
          window.ipc.postMessage({ action: 'copy', path: item.Path });
          window.showToast('Copied full path to clipboard');
        }
        if (action === 'open') window.ipc.postMessage({ action: 'open', path: item.Path });
        return;
      }
      const target = e.target.closest('.row, .file-card');
      if (target && target.dataset.idx !== undefined) {
        const idx = parseInt(target.dataset.idx, 10);
        this.select(idx, e.ctrlKey || e.metaKey);
      }
    });

    this.visible.addEventListener('dblclick', (e) => {
      const target = e.target.closest('.row, .file-card');
      if (target && target.dataset.idx !== undefined) {
        const idx = parseInt(target.dataset.idx, 10);
        if (this.onOpen && this.items[idx]) this.onOpen(this.items[idx]);
      }
    });

    this.visible.addEventListener('contextmenu', (e) => {
      const target = e.target.closest('.row, .file-card');
      if (target && target.dataset.idx !== undefined) {
        e.preventDefault();
        const idx = parseInt(target.dataset.idx, 10);
        this.select(idx);
        if (this.onContext && this.items[idx]) this.onContext(e, this.items[idx]);
      }
    });
  }

  updateViewMetrics() {
    if (this.viewMode === 'card') {
      const containerW = this.container.clientWidth || 1000;
      this.cols = Math.max(1, Math.floor((containerW - 24) / (this.cardWidth + this.cardGap)));
      this.rowHeight = this.cardHeight + this.cardGap;
    } else if (this.viewMode === 'compact') {
      this.cols = 1;
      this.rowHeight = 20;
    } else {
      this.cols = 1;
      this.rowHeight = 24;
    }
  }

  getIconKey(item) {
    if (item.IsFolder) return '__folder__';
    const ext = (item.Ext || '').toLowerCase();
    if (ext === 'exe' || ext === 'lnk' || ext === 'ico') {
      return item.Path; // Unique True Shell Icon per executable/shortcut
    }
    return ext || '__file__';
  }

  setData(items, query = '', resetScroll = true, totalMatches = 0) {
    this.items = items || [];
    this.highlightQuery = query;
    this.totalMatches = Math.max(totalMatches, this.items.length);
    this.loadingMore = false;

    this.updateViewMetrics();
    const isCard = this.viewMode === 'card';
    const totalRows = isCard ? Math.ceil(this.totalMatches / this.cols) : this.totalMatches;
    const fullHeight = Math.min(totalRows * this.rowHeight + 20, 10000000);
    this.spacer.style.height = Math.max(this.container.clientHeight, fullHeight) + 'px';

    if (resetScroll) {
      this.selected = this.items.length ? 0 : -1;
      this.selectedSet.clear();
      if (this.items.length) this.selectedSet.add(0);
      this.container.scrollTop = 0;
    } else {
      if (this.selected >= this.items.length) {
        this.selected = this.items.length - 1;
      }
      // Prune selection set to loaded items
      const pruned = new Set();
      this.selectedSet.forEach(i => { if (i < this.items.length) pruned.add(i); });
      this.selectedSet = pruned;
    }
    this.lastStart = -1;
    this.lastEnd = -1;
    this.render();
  }

  appendItems(newItems) {
    if (!newItems || !newItems.length) {
      this.loadingMore = false;
      return;
    }
    this.items = this.items.concat(newItems);
    this.loadingMore = false;
    this.updateViewMetrics();
    const isCard = this.viewMode === 'card';
    const totalRows = isCard ? Math.ceil(this.totalMatches / this.cols) : this.totalMatches;
    const fullHeight = Math.min(totalRows * this.rowHeight + 20, 10000000);
    this.spacer.style.height = Math.max(this.container.clientHeight, fullHeight) + 'px';
    this.render();
  }

  queueRender() {
    if (this.rafId) return;
    this.rafId = requestAnimationFrame(() => {
      this.rafId = null;
      this.render();
      this.checkInfiniteScroll();
    });
  }

  checkInfiniteScroll() {
    if (this.loadingMore || this.items.length >= this.totalMatches) return;
    const threshold = 4000;
    const scrollBottom = this.container.scrollTop + this.container.clientHeight;
    const isCard = this.viewMode === 'card';
    const totalRows = isCard ? Math.ceil(this.items.length / this.cols) : this.items.length;
    const totalHeight = totalRows * this.rowHeight;

    if (scrollBottom >= totalHeight - threshold) {
      this.loadingMore = true;
      if (this.onLoadMore) {
        this.onLoadMore(this.items.length);
      }
    }
  }

  escapeHtml(str) {
    return String(str).replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  highlight(text) {
    if (!this.highlightQuery) return this.escapeHtml(text);
    const q = this.highlightQuery.replace(/(?:ext|type|path):\S+|!\S+/gi, '').replace(/[.][a-z0-9]+$/i, '').trim();
    if (!q) return this.escapeHtml(text);
    const safe = this.escapeHtml(text);
    const needle = this.escapeHtml(q).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    try {
      return safe.replace(new RegExp('(' + needle + ')', 'ig'), '<span class="match">$1</span>');
    } catch {
      return safe;
    }
  }

  render() {
    this.updateViewMetrics();
    const total = this.totalMatches || this.items.length;

    if (!this.items.length && !total) {
      this.lastStart = -1;
      this.lastEnd = -1;
      this.lastSel = -1;
      this.visible.style.transform = 'translate3d(0,0,0)';
      this.visible.innerHTML = '<div class="empty">No items match your search.</div>';
      return;
    }

    if (this.viewMode === 'card') {
      const totalRows = Math.ceil(total / this.cols);
      const fullHeight = Math.min(totalRows * this.rowHeight + 20, 10000000);
      this.spacer.style.height = Math.max(this.container.clientHeight, fullHeight) + 'px';

      const scrollTop = this.container.scrollTop;
      const startRow = Math.max(0, Math.floor(scrollTop / this.rowHeight) - 2);
      const countRows = Math.ceil(this.container.clientHeight / this.rowHeight) + 5;
      const endRow = Math.min(totalRows, startRow + countRows);

      if (startRow === this.lastStart && endRow === this.lastEnd && this.selected === this.lastSel) {
        return;
      }

      this.lastStart = startRow;
      this.lastEnd = endRow;
      this.lastSel = this.selected;
      this.visible.style.transform = `translate3d(0, ${startRow * this.rowHeight}px, 0)`;
      this.visible.style.height = ((endRow - startRow) * this.rowHeight) + 'px';

      let html = '';
      for (let r = startRow; r < endRow; r++) {
        const rowTop = (r - startRow) * this.rowHeight;
        const startIdx = r * this.cols;
        const endIdx = Math.min(total, (r + 1) * this.cols);

        html += `<div class="card-grid-row" style="top:${rowTop}px">`;
        for (let i = startIdx; i < endIdx; i++) {
          const item = this.items[i];
          if (!item) {
            html += `
              <div class="file-card placeholder-card" data-idx="${i}">
                <div class="card-icon-wrap"><span class="fallback-icon">📄</span></div>
                <div class="card-content">
                  <div class="card-name" style="color:#999">Loading item ${i + 1}...</div>
                </div>
              </div>
            `;
            continue;
          }

          const isSelected = this.selectedSet.has(i);
          const key = this.getIconKey(item);
          const ext = (item.Ext || '').toLowerCase();
          let iconSrc = this.iconCache[key] || this.iconCache[ext] || (item.IsFolder ? this.iconCache.__folder__ : this.iconCache.__file__);
          const iconHtml = iconSrc ? `<img src="${iconSrc}">` : `<span class="fallback-icon">${item.IsFolder ? '📁' : '📄'}</span>`;
          const badge = getCategoryBadge(item);

          html += `
            <div class="file-card ${isSelected ? 'selected' : ''}" data-idx="${i}">
              <div class="card-icon-wrap" data-icon-key="${this.escapeHtml(key)}">${iconHtml}</div>
              <div class="card-content">
                <div class="card-header-line">
                  <span class="card-badge ${badge.cls}">${badge.label}</span>
                  ${i === 0 && this.highlightQuery ? '<span style="font-size:10px;font-weight:700;color:#0078d7">★ Best Match</span>' : ''}
                </div>
                <div class="card-name" title="${this.escapeHtml(item.Name)}">${this.highlight(item.Name)}</div>
                <div class="card-path" title="${this.escapeHtml(item.Path)}">${this.escapeHtml(item.Path)}</div>
              </div>
              <div class="card-actions">
                <button class="card-act-btn" data-action="folder" data-idx="${i}" title="Open Containing Folder">📂</button>
                <button class="card-act-btn" data-action="copy" data-idx="${i}" title="Copy Full Path">📋</button>
                <button class="card-act-btn" data-action="open" data-idx="${i}" title="Open File">⚡</button>
              </div>
            </div>
          `;
        }
        html += `</div>`;
      }

      this.visible.innerHTML = html;
      this.hydrateIcons();
      return;
    }

    // List / Compact Mode Render
    const fullHeight = Math.min(total * this.rowHeight, 10000000);
    this.spacer.style.height = Math.max(this.container.clientHeight, fullHeight) + 'px';

    const scrollTop = this.container.scrollTop;
    const start = Math.max(0, Math.floor(scrollTop / this.rowHeight) - 15);
    const count = Math.ceil(this.container.clientHeight / this.rowHeight) + 30;
    const end = Math.min(total, start + count);

    if (start === this.lastStart && end === this.lastEnd && this.selected === this.lastSel) {
      return;
    }

    this.lastStart = start;
    this.lastEnd = end;
    this.lastSel = this.selected;
    this.visible.style.transform = `translate3d(0, ${start * this.rowHeight}px, 0)`;
    this.visible.style.height = ((end - start) * this.rowHeight) + 'px';

    let html = '';
    for (let i = start; i < end; i++) {
      const item = this.items[i];
      if (!item) {
        html += `
          <div class="row placeholder-row" style="top:${(i - start) * this.rowHeight}px" data-idx="${i}">
            <div style="color:#999"><span class="glyph">📄</span>Loading item ${i + 1}...</div>
            <div class="path"></div>
            <div class="type"></div>
          </div>
        `;
        continue;
      }

      const isSelected = this.selectedSet.has(i);
      const key = this.getIconKey(item);
      const ext = (item.Ext || '').toLowerCase();

      let iconSrc = this.iconCache[key] || this.iconCache[ext] || (item.IsFolder ? this.iconCache.__folder__ : this.iconCache.__file__);
      const iconHtml = iconSrc ? `<img src="${iconSrc}">` : `<span class="fallback-icon">${item.IsFolder ? '📁' : '📄'}</span>`;

      html += `
        <div class="row ${isSelected ? 'selected' : ''}" style="top:${(i - start) * this.rowHeight}px" data-idx="${i}">
          <div>
            <span class="glyph" data-icon-key="${this.escapeHtml(key)}">${iconHtml}</span>
            ${this.highlight(item.Name)}
            ${i === 0 && this.highlightQuery ? ' <small style="color:#0878d1">Best match</small>' : ''}
          </div>
          <div class="path" title="${this.escapeHtml(item.Path)}">${this.escapeHtml(item.Path)}</div>
          <div class="type">${this.escapeHtml(item.IsFolder ? 'Folder' : (item.Ext ? item.Ext.toUpperCase() : 'File'))}</div>
        </div>
      `;
    }

    this.visible.innerHTML = html;
    this.hydrateIcons();
  }

  select(idx, additive = false) {
    if (idx < 0 || idx >= this.items.length) return;
    if (additive) {
      // Ctrl/Cmd+click: toggle this item in the multi-selection set
      if (this.selectedSet.has(idx)) {
        this.selectedSet.delete(idx);
      } else {
        this.selectedSet.add(idx);
      }
      this.selected = idx;
      this.render();
      if (this.onSelect) this.onSelect(this.items[idx]);
      return;
    }
    this.selectedSet.clear();
    this.selectedSet.add(idx);
    this.selected = idx;
    this.render();
    if (this.onSelect) this.onSelect(this.items[idx]);
  }

  setViewMode(mode) {
    if (!['list', 'card', 'compact'].includes(mode)) mode = 'list';
    this.viewMode = mode;
    localStorage.setItem('aizen_view_mode', mode);

    const pane = $('pane');
    if (pane) {
      pane.classList.toggle('card-view', mode === 'card');
      pane.classList.toggle('compact-view', mode === 'compact');
    }

    const headList = $('headList');
    const headCards = $('headCards');
    if (headList) headList.style.display = mode === 'card' ? 'none' : 'grid';
    if (headCards) headCards.style.display = mode === 'card' ? 'flex' : 'none';

    // Toolbar active states
    const btnList = $('btnViewList');
    const btnCard = $('btnViewCard');
    const btnCompact = $('btnViewCompact');
    if (btnList) btnList.classList.toggle('active', mode === 'list');
    if (btnCard) btnCard.classList.toggle('active', mode === 'card');
    if (btnCompact) btnCompact.classList.toggle('active', mode === 'compact');

    // Menu checkmark states
    const mvList = $('menuViewList');
    const mvCard = $('menuViewCard');
    const mvCompact = $('menuViewCompact');
    if (mvList) mvList.textContent = mode === 'list' ? '✓' : '';
    if (mvCard) mvCard.textContent = mode === 'card' ? '✓' : '';
    if (mvCompact) mvCompact.textContent = mode === 'compact' ? '✓' : '';

    this.updateViewMetrics();
    this.lastStart = -1;
    this.lastEnd = -1;
    this.lastSel = -1;
    this.render();
    if (window.showToast) {
      const name = mode === 'card' ? 'Card / Tile Grid' : (mode === 'compact' ? 'Compact List' : 'Details List');
      window.showToast(`View switched to ${name}`);
    }
  }

  hydrateIcons() {
    clearTimeout(this.iconTimer);
    this.iconTimer = setTimeout(() => {
      const requests = [];
      const isCard = this.viewMode === 'card';
      const start = isCard ? Math.max(0, this.lastStart * this.cols) : Math.max(0, this.lastStart);
      const end = isCard ? Math.min(this.items.length, (this.lastEnd + 1) * this.cols) : Math.min(this.items.length, this.lastStart + 60);

      for (let i = start; i < end; i++) {
        const item = this.items[i];
        if (!item) continue;
        const key = this.getIconKey(item);
        if (!this.iconCache[key] && !this.iconPending.has(key)) {
          this.iconPending.add(key);
          requests.push({
            key: key,
            path: item.Path,
            isFolder: !!item.IsFolder,
            ext: (item.Ext || '').toLowerCase()
          });
        }
      }

      if (requests.length === 0) return;
      if (this.iconBusy) return;

      if (window.ipc) {
        this.iconBusy = true;
        window.ipc.postMessage({ action: 'icons', requests });
      }
    }, 40);
  }

  receiveIcons(map) {
    this.iconBusy = false;
    if (!map) return;
    let anyApplied = false;
    Object.keys(map).forEach(k => {
      this.iconPending.delete(k);
      this.iconCache[k] = map[k];
      anyApplied = true;
    });
    if (anyApplied) {
      this.applyIcons();
    }
  }

  applyIcons() {
    const targets = this.visible.querySelectorAll('.glyph[data-icon-key], .card-icon-wrap[data-icon-key]');
    for (let i = 0; i < targets.length; i++) {
      const el = targets[i];
      const key = el.dataset.iconKey;
      const src = this.iconCache[key];
      if (src) {
        const currentImg = el.querySelector('img');
        if (!currentImg || currentImg.src !== src) {
          el.innerHTML = `<img src="${src}">`;
        }
      }
    }
  }
}

// ------------------------------------------------------------------------------
// PreviewController: Rich Media, Audio, Video, Image, Exe, Folder & Text Inspector
// ------------------------------------------------------------------------------
class PreviewController {
  constructor(paneId, boxId, nameId, typeId, sizeId, pathId, createdId, modId, btnId) {
    this.pane = document.getElementById(paneId);
    this.box = document.getElementById(boxId);
    this.nameEl = document.getElementById(nameId);
    this.typeEl = document.getElementById(typeId);
    this.sizeEl = document.getElementById(sizeId);
    this.pathEl = document.getElementById(pathId);
    this.createdEl = document.getElementById(createdId);
    this.modEl = document.getElementById(modId);
    this.btnEl = document.getElementById(btnId);
    this.requestedPath = '';
  }

  toggle() {
    this.pane.classList.toggle('preview-on');
    const active = this.isOpen();
    if (this.btnEl) this.btnEl.classList.toggle('active', active);
  }

  isOpen() {
    return this.pane.classList.contains('preview-on');
  }

  show(p) {
    if (!p) {
      this.clear();
      return;
    }
    // A slow remote read can finish after the user has selected another item.
    // Ignore that stale response rather than replacing the current preview.
    if (this.requestedPath && p.Path && p.Path !== this.requestedPath) return;

    this.nameEl.textContent = p.Name || 'No selection';
    this.typeEl.textContent = p.Extension ? `${p.Extension} (${p.Kind})` : (p.Kind || '—');
    this.sizeEl.textContent = p.Size || '—';
    this.pathEl.textContent = p.Path || '—';
    if (this.createdEl) this.createdEl.textContent = p.Created || '—';
    if (this.modEl) this.modEl.textContent = p.Modified || '—';

    this.box.innerHTML = '';

    if (p.Kind === 'image' && p.Content) {
      const img = document.createElement('img');
      img.className = 'preview-img';
      img.src = p.Content;
      img.alt = p.Name;
      this.box.appendChild(img);
    } else if (p.Kind === 'image') {
      this.box.innerHTML = `
        <div class="media-player-box">
          ${p.IconBase64 ? `<img class="file-card-icon" src="${p.IconBase64}">` : `<div class="audio-disc-icon">🖼️</div>`}
          <div class="media-title">${this.escape(p.Name)}</div>
          <div class="file-card-size">${this.escape(p.Size)}</div>
          <div class="file-card-extra">${this.escape(p.ExtraInfo || 'Image (Click Open to view)')}</div>
        </div>
      `;
    } else if (p.Kind === 'audio') {
      this.box.innerHTML = `
        <div class="media-player-box">
          <div class="audio-disc-icon">🎵</div>
          <div class="media-title">${this.escape(p.Name)}</div>
          ${p.Content ? `<audio controls autoplay src="${p.Content}"></audio>` : `
            <div class="file-card-size">${this.escape(p.Size || 'Remote audio')}</div>
            <div class="file-card-extra">${this.escape(p.ExtraInfo || 'Audio preview unavailable — click Open to play from the connected location.')}</div>
          `}
        </div>
      `;
    } else if (p.Kind === 'video') {
      this.box.innerHTML = `
        <div class="media-player-box">
          ${p.Content ? `<video controls autoplay src="${p.Content}"></video>` : `
            <div style="font-size:40px;margin-bottom:10px">🎬</div>
            <div class="media-title">${this.escape(p.Name)}</div>
            <div class="file-card-size">${this.escape(p.Size || 'Remote video')}</div>
            <div class="file-card-extra">${this.escape(p.ExtraInfo || 'Video preview unavailable — click Open to play from the connected location.')}</div>
          `}
        </div>
      `;
    } else if (p.Kind === 'exe') {
      this.box.innerHTML = `
        <div class="exe-info-box">
          ${p.IconBase64 ? `<img class="exe-large-icon" src="${p.IconBase64}">` : `<div class="exe-badge">EXE</div>`}
          <div class="exe-title">${this.escape(p.Name)}</div>
          <div class="exe-details"><pre>${this.escape(p.ExtraInfo || 'Windows Executable Application')}</pre></div>
        </div>
      `;
    } else if (p.Kind === 'folder') {
      this.box.innerHTML = `
        <div class="folder-info-box">
          ${p.IconBase64 ? `<img class="folder-large-icon" src="${p.IconBase64}">` : `<div class="folder-badge">📁</div>`}
          <div class="folder-title">${this.escape(p.Name)}</div>
          <div class="folder-details">${this.escape(p.ExtraInfo || p.Size || 'Folder')}</div>
        </div>
      `;
    } else if (p.Kind === 'text' && p.Content) {
      const pre = document.createElement('pre');
      pre.className = 'preview-code';
      pre.textContent = p.Content;
      this.box.appendChild(pre);
    } else if (p.Kind === 'text') {
      this.box.innerHTML = `
        <div class="file-card-box">
          ${p.IconBase64 ? `<img class="file-card-icon" src="${p.IconBase64}">` : `<div class="file-card-badge">TXT</div>`}
          <div class="file-card-name">${this.escape(p.Name)}</div>
          <div class="file-card-size">${this.escape(p.Size)}</div>
          <div class="file-card-extra">${this.escape(p.ExtraInfo || 'Text Document (Click Open to view)')}</div>
        </div>
      `;
    } else {
      this.box.innerHTML = `
        <div class="file-card-box">
          ${p.IconBase64 ? `<img class="file-card-icon" src="${p.IconBase64}">` : `<div class="file-card-badge">${this.escape(p.Extension || 'FILE')}</div>`}
          <div class="file-card-name">${this.escape(p.Name)}</div>
          <div class="file-card-size">${this.escape(p.Size)}</div>
          ${p.ExtraInfo ? `<div class="file-card-extra">${this.escape(p.ExtraInfo)}</div>` : ''}
        </div>
      `;
    }
  }

  escape(str) {
    return String(str || '').replace(/[&<>"']/g, c => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  clear() {
    this.nameEl.textContent = 'No selection';
    this.typeEl.textContent = '—';
    this.sizeEl.textContent = '—';
    this.pathEl.textContent = '—';
    if (this.createdEl) this.createdEl.textContent = '—';
    if (this.modEl) this.modEl.textContent = '—';
    this.box.innerHTML = '<span class="preview-placeholder">Select a file to preview</span>';
  }
}

// A preview request must always produce visible feedback, even when a remote
// A connected location is offline or takes too long to answer.
window.previewError = (data) => {
  if (!data) return;
  const msg = String(data.message || 'Remote file is unavailable right now.');
  const path = String(data.path || '');
  const name = path.split('\\').filter(Boolean).pop() || 'File';
  const nameEl = $('pname');
  const pathEl = $('ppath');
  const box = $('pbox');
  if (nameEl) nameEl.textContent = name;
  if (pathEl) pathEl.textContent = path;
  if (box) box.innerHTML = `<div class="preview-error"><strong>Preview unavailable</strong><div>${grid.escapeHtml(msg)}</div><small>Check that the connected location is available, then select the file again.</small></div>`;
};

// ------------------------------------------------------------------------------
// Application State & Controller
// ------------------------------------------------------------------------------
let currentKind = 'all';
let currentExt = '';
let currentSort = 'name';
let currentDesc = false;
let matchCase = false;
let wholeWord = false;
let matchPath = true;
let regexMode = false;
// Feature #3: size & date filters (bytes / ISO dates)
let currentMinSize = null;   // bytes
let currentMaxSize = null;   // bytes
let currentDateAfter = null; // ISO string
let currentDateBefore = null; // ISO string
// Feature #5: favorites / pinning (persisted)
const FAV_KEY = 'aizen_favorites';
let favorites = [];
try { favorites = JSON.parse(localStorage.getItem(FAV_KEY) || '[]'); } catch { favorites = []; }
if (!Array.isArray(favorites)) favorites = [];

function saveFavorites() {
  try { localStorage.setItem(FAV_KEY, JSON.stringify(favorites)); } catch { }
}

function isFavorite(path) {
  return favorites.includes(path);
}

function updateCtxPinBtn() {
  const btn = $('ctxPinBtn');
  const cur = grid.items[grid.selected];
  if (btn && cur) {
    btn.textContent = isFavorite(cur.Path) ? '★ Unpin from Favorites' : '⭐ Pin to Favorites';
  }
}

window.toggleFavorite = () => {
  const cur = grid.items[grid.selected];
  if (!cur) return;
  const path = cur.Path;
  if (isFavorite(path)) {
    favorites = favorites.filter(p => p !== path);
    window.showToast('Removed from favorites');
  } else {
    favorites.unshift(path);
    if (favorites.length > 500) favorites.length = 500;
    window.showToast('⭐ Added to favorites');
  }
  saveFavorites();
  updateCtxPinBtn();
};

window.showFavorites = () => {
  if (!favorites.length) {
    window.showToast('No favorites yet — right-click a file and choose "Pin to Favorites"');
    return;
  }
  // Search for all favorite paths via a special query marker
  $('q').value = 'fav:all';
  $('q').focus();
  window.ipc.postMessage({
    action: 'search',
    search_id: ++searchSeq,
    query: 'fav:all',
    kind: 'all',
    ext: '',
    offset: 0,
    limit: 1000,
    reset_scroll: true,
    favorites: favorites
  });
};

// Feature #15: large file finder — files ≥ 1 GB
window.findLargeFiles = () => {
  $('q').value = 'size:>1gb';
  $('q').focus();
  currentMinSize = 1024 * 1024 * 1024;
  currentMaxSize = null;
  if ($('minSize')) $('minSize').value = '1024';
  if ($('maxSize')) $('maxSize').value = '';
  triggerSearch(true);
  window.showToast('Showing files larger than 1 GB');
};

// Feature #19: open diagnostics log folder
window.openLogFolder = () => {
  window.ipc.postMessage({ action: 'open_log_folder' });
  window.showToast('Opening log folder');
};

// Feature #14: duplicate finder
window.findDuplicates = () => {
  $('q').value = 'dups:all';
  $('q').focus();
  window.ipc.postMessage({
    action: 'find_duplicates',
    search_id: ++searchSeq
  });
  $('status').textContent = 'Finding duplicate files...';
};

// Feature #13: recently modified (last 7 days)
window.findRecent = () => {
  $('q').value = 'date:after ' + new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10);
  $('q').focus();
  currentDateAfter = new Date(Date.now() - 7 * 86400000).toISOString();
  currentDateBefore = null;
  if ($('dateAfter')) $('dateAfter').value = new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10);
  if ($('dateBefore')) $('dateBefore').value = '';
  triggerSearch(true);
  window.showToast('Showing files modified in the last 7 days');
};
// Feature #8: theme toggle (persisted)
const THEME_KEY = 'aizen_theme';
function applyTheme(theme) {
  document.body.classList.toggle('dark-theme', theme === 'dark');
  try { localStorage.setItem(THEME_KEY, theme); } catch { }
  const btn = $('themeToggle');
  if (btn) btn.textContent = theme === 'dark' ? '☀️ Light' : '🌙 Dark';
}
window.toggleTheme = () => {
  const dark = document.body.classList.contains('dark-theme');
  applyTheme(dark ? 'light' : 'dark');
  window.showToast(dark ? 'Light theme' : 'Dark theme');
};
function loadTheme() {
  try {
    const t = localStorage.getItem(THEME_KEY);
    applyTheme(t === 'dark' ? 'dark' : 'light');
  } catch { applyTheme('light'); }
}
// Feature #7: search option persistence (localStorage)
const OPTS_KEY = 'aizen_search_opts';
function loadSearchOptions() {
  try {
    const o = JSON.parse(localStorage.getItem(OPTS_KEY) || '{}');
    if (typeof o.matchCase === 'boolean') matchCase = o.matchCase;
    if (typeof o.wholeWord === 'boolean') wholeWord = o.wholeWord;
    if (typeof o.matchPath === 'boolean') matchPath = o.matchPath;
    if (typeof o.regexMode === 'boolean') regexMode = o.regexMode;
    if (typeof o.sort === 'string') currentSort = o.sort;
    if (typeof o.desc === 'boolean') currentDesc = o.desc;
    if (typeof o.kind === 'string') currentKind = o.kind;
  } catch { }
}
function saveSearchOptions() {
  try {
    localStorage.setItem(OPTS_KEY, JSON.stringify({
      matchCase, wholeWord, matchPath, regexMode,
      sort: currentSort, desc: currentDesc, kind: currentKind
    }));
  } catch { }
}
// Feature #4: search history (persisted in localStorage)
const SEARCH_HISTORY_KEY = 'aizen_search_history';
let searchHistory = [];
try { searchHistory = JSON.parse(localStorage.getItem(SEARCH_HISTORY_KEY) || '[]'); } catch { searchHistory = []; }
if (!Array.isArray(searchHistory)) searchHistory = [];

function addSearchHistory(query) {
  const q = (query || '').trim();
  if (!q) return;
  searchHistory = searchHistory.filter(h => h !== q);
  searchHistory.unshift(q);
  if (searchHistory.length > 20) searchHistory.length = 20;
  try { localStorage.setItem(SEARCH_HISTORY_KEY, JSON.stringify(searchHistory)); } catch { }
  renderSearchHistory();
}

function renderSearchHistory() {
  const el = $('searchHistoryList');
  if (!el) return;
  if (!searchHistory.length) {
    el.innerHTML = '<div class="history-empty">No recent searches yet</div>';
    return;
  }
  el.innerHTML = searchHistory.map(h =>
    `<div class="history-item" onclick="window.applySearchPreset('${h.replace(/'/g, "\\'")}')" title="${h}">
       <span class="history-icon">🕘</span><span class="history-text">${grid.escapeHtml(h)}</span>
       <button class="history-del" onclick="event.stopPropagation();window.removeSearchHistory('${h.replace(/'/g, "\\'")}')" title="Remove">✕</button>
     </div>`
  ).join('');
}

window.removeSearchHistory = (q) => {
  searchHistory = searchHistory.filter(h => h !== q);
  try { localStorage.setItem(SEARCH_HISTORY_KEY, JSON.stringify(searchHistory)); } catch { }
  renderSearchHistory();
};

window.clearSearchHistory = () => {
  searchHistory = [];
  try { localStorage.setItem(SEARCH_HISTORY_KEY, '[]'); } catch { }
  renderSearchHistory();
  window.showToast('Search history cleared');
};


let searchTimer = null;
let searchWatch = null;
let searchSeq = 0;
let latestApplied = 0;
let currentGhostSuggestion = '';
let searchTimedOut = false;

const grid = new VirtualGrid('rows', 'spacer', 'visible', 24);
const preview = new PreviewController('pane', 'pbox', 'pname', 'ptype', 'psize', 'ppath', 'pcreated', 'pmod', 'previewBtn');

grid.onSelect = (item) => {
  $('selectedPath').textContent = item ? item.Path : '';
  preview.requestedPath = item?.Path || '';
  if (item && preview.isOpen()) {
    window.ipc.postMessage({ action: 'preview', path: item.Path });
  }
};

grid.onOpen = (item) => {
  if (item) {
    window.ipc.postMessage({ action: 'open', path: item.Path });
  }
};

grid.onContext = (e, item) => {
  const ctx = $('ctx');
  ctx.style.left = Math.min(e.clientX, window.innerWidth - 220) + 'px';
  ctx.style.top = Math.min(e.clientY, window.innerHeight - 160) + 'px';
  ctx.classList.add('show');
  updateCtxPinBtn();
};

grid.onLoadMore = (offset) => {
  window.ipc.postMessage({
    action: 'search',
    search_id: searchSeq,
    query: $('q').value,
    kind: currentKind,
    ext: currentExt,
    offset: offset,
    limit: 1000,
    append: true,
    sort: currentSort,
    descending: currentDesc,
    match_case: matchCase,
    whole_word: wholeWord,
    match_path: matchPath,
    regex: regexMode,
    min_size: currentMinSize,
    max_size: currentMaxSize,
    modified_after: currentDateAfter,
    modified_before: currentDateBefore
  });
};

function hideContext() {
  $('ctx').classList.remove('show');
}

document.addEventListener('click', (e) => {
  if (!e.target.closest('#ctx')) hideContext();
});

function triggerSearch(resetScroll = true) {
  clearTimeout(searchTimer);
  const q = $('q').value;
  $('queryHelp').classList.toggle('show', /^(ext:|type:|path:|!)/i.test(q));

  const shouldReset = (typeof resetScroll === 'boolean') ? resetScroll : true;
  const id = ++searchSeq;
  $('status').textContent = 'Searching...';

  searchTimer = setTimeout(() => {
    window.ipc.postMessage({
      action: 'search',
      search_id: id,
      query: q,
      kind: currentKind,
      ext: currentExt,
      offset: 0,
      limit: 1000,
      reset_scroll: shouldReset,
      sort: currentSort,
      descending: currentDesc,
      match_case: matchCase,
      whole_word: wholeWord,
      match_path: matchPath,
      regex: regexMode,
      min_size: currentMinSize,
      max_size: currentMaxSize,
      modified_after: currentDateAfter,
      modified_before: currentDateBefore
    });

    clearTimeout(searchWatch);
    searchWatch = setTimeout(() => {
      if (latestApplied < id && searchSeq === id) {
        searchTimedOut = true;
        $('status').textContent = 'Search timed out - press Enter to retry';
      }
    }, 3000);
  }, 20);
}

// Realtime Index Change Callback: Updates total count without stealing scroll focus!
window.realtimeIndexChanged = (data) => {
  if (data && data.count) {
    $('totalCount').textContent = `${Number(data.count).toLocaleString()} indexed`;
  }
  // NEVER disrupt user's scroll position or active search!
  // Only silently refresh if user is resting at the top, idle, and not actively searching
  if (grid.container.scrollTop === 0 && !grid.loadingMore && !grid.isScrolling && !$('q').value) {
    triggerSearch(false);
  }
};

// C# IPC Callback entry points
window.showSearchResponse = (res) => {
  if (res.id !== searchSeq) return;
  clearTimeout(searchWatch);
  latestApplied = res.id;
  searchTimedOut = false;
  $('status').textContent = `Ready - ${Number(res.ms).toFixed(0)} ms`;
  const total = res.total ?? res.items.length;
  grid.totalMatches = total;
  $('count').textContent = `${Number(res.items.length).toLocaleString()} of ${Number(total).toLocaleString()} results`;
  if ($('cardStats')) {
    $('cardStats').textContent = `${Number(res.items.length).toLocaleString()} cards (${Number(total).toLocaleString()} matches)`;
  }

  const shouldReset = (res.resetScroll !== undefined) ? res.resetScroll : (grid.container.scrollTop === 0);
  grid.setData(res.items, $('q').value, shouldReset, total);

  updateAutocomplete(res.items);

  if (shouldReset && res.items[0]) {
    grid.select(0);
  } else if (!res.items.length) {
    preview.clear();
  }
};

// Append (infinite scroll) search response: called by C# when a "Load More"
// page of results arrives. Appends to the grid without resetting scroll.
window.appendSearchResponse = (res) => {
  if (res.id !== searchSeq) return;
  clearTimeout(searchWatch);
  latestApplied = res.id;
  const total = res.total ?? res.items.length;
  grid.totalMatches = total;
  const shownCount = grid.items.length + res.items.length;
  $('count').textContent = `${Number(shownCount).toLocaleString()} of ${Number(total).toLocaleString()} results`;
  if ($('cardStats')) {
    $('cardStats').textContent = `${Number(shownCount).toLocaleString()} cards (${Number(total).toLocaleString()} matches)`;
  }
  grid.appendItems(res.items);
  if (!res.items.length) {
    grid.loadingMore = false;
  }
};

// Toggle preview pane (Alt+P, Preview button, context menu, --preview arg)
window.togglePreview = () => {
  preview.toggle();
  const active = preview.isOpen();
  const check = $('menuCheckPreview');
  if (check) check.textContent = active ? '✓' : '';
  const btn = $('previewBtn');
  if (btn) btn.classList.toggle('active', active);
};

// Drives changed callback: new drive mounted / drive removed.
// Updates the status line and, when the index overlay is visible, its drive list.
window.drivesUpdated = (drives) => {
  $('status').textContent = 'Detected new drive(s) - updating index...';
  if ($('indexOverlay') && $('indexOverlay').classList.contains('show')) {
    $('driveStates').innerHTML = (drives || []).map(d => `<div class="drive-state" id="drv-${String(d[0])}"><b>${d}</b> Queued</div>`).join('');
  }
};

window.showPreview = (data) => {
  preview.show(data);
};



window.showIcons = (map) => {
  grid.receiveIcons(map);
};

window.setSearchQuery = (text) => {
  $('q').value = text;
  triggerSearch();
  $('q').focus();
};

window.boot = (payload) => {
  grid.setViewMode(grid.viewMode);
  // Feature #8: restore theme
  loadTheme();
  // Feature #7: restore persisted search options
  loadSearchOptions();
  if ($('filter')) $('filter').value = currentKind;
  document.querySelectorAll('.filter-chip').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.filter === currentKind);
  });
  ['sn', 'sp', 'st'].forEach(id => { const el = $(id); if (el) el.textContent = ''; });
  const indicator = currentDesc ? '▼' : '▲';
  if (currentSort === 'name' && $('sn')) $('sn').textContent = indicator;
  if (currentSort === 'path' && $('sp')) $('sp').textContent = indicator;
  if (currentSort === 'type' && $('st')) $('st').textContent = indicator;
  window.updateMenuStateIndicators();
  $('totalCount').textContent = `${Number(payload.count).toLocaleString()} indexed`;
  $('count').textContent = '0 results';
  $('status').textContent = payload.cached ? `Ready - ${payload.engine}` : `Indexing - ${payload.engine}`;
  $('flags').textContent = payload.elevated ? 'ADMIN - MFT' : 'SCAN FALLBACK';
  $('mftbtn').style.display = payload.elevated ? 'none' : 'inline-block';
  $('q').focus();

  if (payload.cached) {
    triggerSearch();
  }
};

window.indexStarted = (x) => {
  $('indexOverlay').classList.add('show');
  $('indexFill').style.width = '2%';
  $('indexPercent').textContent = '0%';
  $('indexSummary').textContent = `Deep indexing ${x.totalDrives} local volumes`;
  $('driveStates').innerHTML = x.drives.map(d => `<div class="drive-state" id="drv-${d[0]}"><b>${d}</b> Queued</div>`).join('');
  $('status').textContent = `Refreshing deep index - cached ${Number(x.cachedCount).toLocaleString()} available`;
};

window.indexDriveProgress = (x) => {
  const completed = x.completed ?? x.Completed ?? 0;
  const totalDrives = x.totalDrives ?? x.TotalDrives ?? 1;
  const count = x.count ?? x.Count ?? 0;
  const seconds = x.seconds ?? x.Seconds ?? 0;
  const drive = x.drive ?? x.Drive ?? '';
  const stage = x.stage ?? x.Stage ?? '';
  const engine = x.engine ?? x.Engine ?? '';

  const pct = Math.round((completed / totalDrives) * 100);
  $('indexFill').style.width = Math.max(3, pct) + '%';
  $('indexPercent').textContent = pct + '%';
  $('indexSummary').textContent = `${Number(count).toLocaleString()} objects - ${completed}/${totalDrives} volumes - ${Number(seconds).toFixed(1)}s`;
  const el = $('drv-' + (drive[0] || ''));
  if (el) el.innerHTML = `<b>${drive}</b> ${stage}${engine ? ' - ' + engine : ''}`;
  $('indexDetail').textContent = x.detail || x.Detail || 'Cached results remain searchable during refresh.';
  $('status').textContent = `Indexing ${drive} - ${stage}`;
};

// Feature #10: cancel current indexing operation
window.cancelIndexing = () => {
  window.ipc.postMessage({ action: 'cancel_indexing' });
  $('status').textContent = 'Cancelling indexing...';
  const btn = $('indexCancelBtn');
  if (btn) {
    btn.textContent = 'Cancelling…';
    btn.disabled = true;
  }
};

window.indexCancelled = () => {
  const btn = $('indexCancelBtn');
  if (btn) {
    btn.textContent = '✕ Cancel indexing';
    btn.disabled = false;
  }
  $('status').textContent = 'Indexing cancelled';
  setTimeout(() => $('indexOverlay').classList.remove('show'), 1200);
  window.showToast('Indexing cancelled');
};

window.indexReady = (x) => {
  $('indexFill').style.width = '100%';
  $('indexPercent').textContent = '100%';
  $('indexSummary').textContent = `${Number(x.count).toLocaleString()} objects indexed in ${Number(x.seconds).toFixed(1)}s`;
  $('indexDetail').textContent = x.engines ? x.engines.join(' - ') : 'Fresh index complete';
  $('status').textContent = `Ready - Fresh deep index - ${x.engines ? x.engines.join(' - ') : 'complete'}`;
  $('count').textContent = `${Number(x.count).toLocaleString()} objects`;
  setTimeout(() => $('indexOverlay').classList.remove('show'), 3500);

  // Only trigger search if grid is currently empty and user is not searching
  if (grid.items.length === 0 && !$('q').value) {
    triggerSearch(true);
  }
};

window.act = (action) => {
  hideContext();
  const selectedItem = grid.items[grid.selected];
  if (!selectedItem) return;

  if (action === 'open') {
    window.ipc.postMessage({ action: 'open', path: selectedItem.Path });
  } else if (action === 'folder') {
    window.ipc.postMessage({ action: 'folder', path: selectedItem.Path });
  } else if (action === 'copy') {
    window.ipc.postMessage({ action: 'copy', path: selectedItem.Path });
  } else if (action === 'copy_name') {
    window.ipc.postMessage({ action: 'copy_name', name: selectedItem.Name });
  }
};



function updateAutocomplete(items) {
  const ghostEl = $('ghostSuggestion');
  if (!ghostEl) return;
  const q = $('q').value;
  if (!q || !q.trim() || !items || items.length === 0) {
    currentGhostSuggestion = '';
    ghostEl.textContent = '';
    ghostEl.classList.remove('show');
    return;
  }
  const top = items[0];
  const topName = top ? top.Name : '';
  if (!topName || topName.toLowerCase() === q.toLowerCase()) {
    currentGhostSuggestion = '';
    ghostEl.textContent = '';
    ghostEl.classList.remove('show');
    return;
  }

  currentGhostSuggestion = topName;
  ghostEl.textContent = topName;
  ghostEl.classList.add('show');
}

function applyGhostSuggestion() {
  if (currentGhostSuggestion) {
    $('q').value = currentGhostSuggestion;
    const ghostEl = $('ghostSuggestion');
    if (ghostEl) {
      ghostEl.textContent = '';
      ghostEl.classList.remove('show');
    }
    currentGhostSuggestion = '';
    triggerSearch(true);
  }
}

window.toggleAbout = (force) => {
  const modal = $('aboutModal');
  if (!modal) return;
  const show = typeof force === 'boolean' ? force : !modal.classList.contains('show');
  modal.classList.toggle('show', show);
  if (show) {
    const total = $('totalCount') ? $('totalCount').textContent : '2,470,000+ files';
    if ($('aboutTotalCount')) $('aboutTotalCount').textContent = total;
  }
};

window.toggleChangelog = (force) => {
  const modal = $('changelogModal');
  if (!modal) return;
  const show = typeof force === 'boolean' ? force : !modal.classList.contains('show');
  modal.classList.toggle('show', show);
};

window.toggleUpdateModal = (force) => {
  const modal = $('updateModal');
  if (!modal) return;
  const show = typeof force === 'boolean' ? force : !modal.classList.contains('show');
  modal.classList.toggle('show', show);
};

window.checkUpdate = () => {
  const btn = $('updatePillBtn');
  if (btn) btn.textContent = '⟳ Checking…';
  window.toggleUpdateModal(true);
  $('updateModalTitle').textContent = 'Checking for Updates';
  $('updateModalSubtitle').textContent = 'Connecting to GitHub repository…';
  $('updateModalBody').innerHTML = `
    <div class="update-state update-state-checking">
      <div class="spinner"></div>
      <p class="update-state-text">Checking the latest release on GitHub…</p>
    </div>
  `;
  window.ipc.postMessage({ action: 'check_update' });
};

window.updateCheckResult = (res) => {
  const pillBtn = $('updatePillBtn');
  if (!res) return;

  if (res.UpdateAvailable) {
    if (pillBtn) {
      pillBtn.textContent = `★ Update v${res.LatestVersion}`;
      pillBtn.style.background = '#107c41';
      pillBtn.style.color = '#fff';
    }

    const banner = $('updateBanner');
    if (banner) {
      $('updateBannerText').textContent = `New version v${res.LatestVersion} is available! (Current: v${res.CurrentVersion})`;
      banner.style.display = 'flex';
    }

    $('updateModalTitle').textContent = 'Update Available!';
    $('updateModalSubtitle').textContent = `AizenRex Search-man v${res.LatestVersion} is ready to install`;
    $('updateModalBody').innerHTML = `
      <div class="update-state update-state-available">
        <div class="update-version-card">
          <strong class="update-version-name">AizenRex Search-man v${grid.escapeHtml(res.LatestVersion)}</strong>
          <div class="update-version-sub">Installed: v${grid.escapeHtml(res.CurrentVersion || '')}</div>
        </div>
        ${res.ReleaseNotes ? `<div class="update-notes">${grid.escapeHtml(res.ReleaseNotes)}</div>` : ''}
        <p class="update-state-text">Download the installer below, or open the release page for the portable build.</p>
      </div>
    `;
    $('updateModalFooter').innerHTML = `
      <button class="modal-secondary-btn" onclick="window.toggleUpdateModal(false)">Later</button>
      <button class="modal-action-btn" onclick="window.ipc.postMessage({action:'open',path:'${res.DownloadUrl || 'https://github.com/aizenrexx/AizenSearch/releases'}'})">Download v${res.LatestVersion}</button>
    `;
  } else {
    if (pillBtn) {
      pillBtn.textContent = '✓ Up to Date';
      setTimeout(() => { if (pillBtn) pillBtn.textContent = '⟳ Updates'; }, 4000);
    }

    $('updateModalTitle').textContent = 'You are Up to Date!';
    $('updateModalSubtitle').textContent = `AizenRex Search-man v${res.CurrentVersion} is the newest release.`;
    $('updateModalBody').innerHTML = `
      <div class="update-state update-state-ok">
        <div class="update-state-icon">✓</div>
        <div class="update-state-title">You have the latest version</div>
        <p class="update-state-text">${grid.escapeHtml(res.Message || 'AizenRex Search-man is up to date.')}</p>
        <p class="update-state-note">Installed: v${grid.escapeHtml(res.CurrentVersion || '')}</p>
      </div>
    `;
    $('updateModalFooter').innerHTML = `
      <button class="modal-action-btn" onclick="window.toggleUpdateModal(false)">Done</button>
    `;
  }
};

window.clearSearch = () => {
  $('q').value = '';
  updateAutocomplete([]);
  triggerSearch();
  $('q').focus();
};

// Feature #3: size/date filter helpers
function parseSizeToBytes(val) {
  if (!val) return null;
  const s = String(val).trim().toLowerCase();
  let mult = 1;
  if (s.endsWith('tb')) { mult = 1024 * 1024 * 1024 * 1024; }
  else if (s.endsWith('gb')) { mult = 1024 * 1024 * 1024; }
  else if (s.endsWith('mb')) { mult = 1024 * 1024; }
  else if (s.endsWith('kb')) { mult = 1024; }
  const num = parseFloat(s.replace(/[a-z]+$/, ''));
  if (isNaN(num) || num < 0) return null;
  return Math.round(num * mult);
}

function readSizeDateFilters() {
  currentMinSize = parseSizeToBytes($('minSize') ? $('minSize').value : '');
  currentMaxSize = parseSizeToBytes($('maxSize') ? $('maxSize').value : '');
  currentDateAfter = $('dateAfter') && $('dateAfter').value ? $('dateAfter').value + 'T00:00:00' : null;
  currentDateBefore = $('dateBefore') && $('dateBefore').value ? $('dateBefore').value + 'T23:59:59' : null;
}

window.clearSizeDateFilters = () => {
  if ($('minSize')) $('minSize').value = '';
  if ($('maxSize')) $('maxSize').value = '';
  if ($('dateAfter')) $('dateAfter').value = '';
  if ($('dateBefore')) $('dateBefore').value = '';
  currentMinSize = null;
  currentMaxSize = null;
  currentDateAfter = null;
  currentDateBefore = null;
  triggerSearch(true);
  window.showToast('Size & date filters cleared');
};

// Wire up size/date filter inputs
['minSize', 'maxSize', 'dateAfter', 'dateBefore'].forEach(id => {
  const el = $(id);
  if (el) {
    el.addEventListener('change', () => {
      readSizeDateFilters();
      triggerSearch(true);
    });
    el.addEventListener('input', () => {
      if (id === 'minSize' || id === 'maxSize') {
        readSizeDateFilters();
        triggerSearch(true);
      }
    });
  }
});

window.setViewMode = (mode) => {
  grid.setViewMode(mode);
};

window.setFilter = (val) => {
  currentKind = val;
  currentExt = '';
  if ($('filter')) $('filter').value = val;
  document.querySelectorAll('.filter-chip').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.filter === val);
  });
  saveSearchOptions();
  triggerSearch(true);
};

window.sortBy = (col) => {
  currentDesc = currentSort === col ? !currentDesc : false;
  currentSort = col;
  saveSearchOptions();
  ['sn', 'sp', 'st'].forEach(id => { const el = $(id); if (el) el.textContent = ''; });
  const indicator = currentDesc ? '▼' : '▲';
  if (col === 'name') $('sn').textContent = indicator;
  if (col === 'path') $('sp').textContent = indicator;
  if (col === 'type') $('st').textContent = indicator;

  ['csName', 'csPath', 'csType'].forEach(id => {
    const el = $(id);
    if (el) {
      el.classList.remove('active');
      el.textContent = id.replace('cs', '');
    }
  });
  const activeCs = $('cs' + col.charAt(0).toUpperCase() + col.slice(1));
  if (activeCs) {
    activeCs.classList.add('active');
    activeCs.textContent = `${col.charAt(0).toUpperCase() + col.slice(1)} ${indicator}`;
  }

  triggerSearch(true);
};

// ==============================================================================
// Dropdown Menu System & Actions
// ==============================================================================
let activeMenu = null;

window.toggleMenu = function(menuName, event) {
  if (event) {
    event.stopPropagation();
    event.preventDefault();
  }
  if (activeMenu === menuName) {
    window.closeMenus();
    return;
  }
  window.openMenu(menuName);
};

window.openMenu = function(menuName) {
  window.closeMenus();
  activeMenu = menuName;
  const id = 'menuItem' + menuName.charAt(0).toUpperCase() + menuName.slice(1);
  const item = document.getElementById(id);
  if (item) item.classList.add('active');
  window.updateMenuStateIndicators();
};

window.closeMenus = function() {
  activeMenu = null;
  document.querySelectorAll('.menu-item').forEach(el => el.classList.remove('active'));
};

document.addEventListener('click', (e) => {
  if (!e.target.closest('.menu-item')) {
    window.closeMenus();
  }
});

// Hover-switching when a menu is already open
document.querySelectorAll('.menu-item').forEach(item => {
  item.addEventListener('mouseenter', () => {
    if (activeMenu) {
      const name = item.id.replace('menuItem', '').toLowerCase();
      window.openMenu(name);
    }
  });
});

window.updateMenuStateIndicators = function() {
  const pCheck = $('menuCheckPreview');
  if (pCheck) pCheck.textContent = $('pane')?.classList.contains('preview-on') ? '✓' : '';

  const caseCheck = $('menuCheckCase');
  if (caseCheck) caseCheck.textContent = matchCase ? '✓' : '';

  const wordCheck = $('menuCheckWord');
  if (wordCheck) wordCheck.textContent = wholeWord ? '✓' : '';

  const pathCheck = $('menuCheckPath');
  if (pathCheck) pathCheck.textContent = matchPath ? '✓' : '';

  const regexCheck = $('menuCheckRegex');
  if (regexCheck) regexCheck.textContent = regexMode ? '✓' : '';

  const sortName = $('menuSortName');
  if (sortName) sortName.textContent = currentSort === 'name' ? '✓' : '';

  const sortPath = $('menuSortPath');
  if (sortPath) sortPath.textContent = currentSort === 'path' ? '✓' : '';

  const sortType = $('menuSortType');
  if (sortType) sortType.textContent = currentSort === 'type' ? '✓' : '';

  const mvList = $('menuViewList');
  if (mvList) mvList.textContent = grid.viewMode === 'list' ? '✓' : '';

  const mvCard = $('menuViewCard');
  if (mvCard) mvCard.textContent = grid.viewMode === 'card' ? '✓' : '';

  const mvCompact = $('menuViewCompact');
  if (mvCompact) mvCompact.textContent = grid.viewMode === 'compact' ? '✓' : '';
};

window.menuSort = function(col) {
  window.closeMenus();
  window.sortBy(col);
  window.updateMenuStateIndicators();
};

window.toggleSearchOption = function(opt) {
  window.closeMenus();
  if (opt === 'match_case') matchCase = !matchCase;
  if (opt === 'whole_word') wholeWord = !wholeWord;
  if (opt === 'match_path') matchPath = !matchPath;
  if (opt === 'regex') regexMode = !regexMode;
  saveSearchOptions();
  window.updateMenuStateIndicators();
  triggerSearch(true);
  const stateStr = (opt === 'match_case' ? matchCase : opt === 'whole_word' ? wholeWord : opt === 'match_path' ? matchPath : regexMode) ? 'ON' : 'OFF';
  window.showToast(`${opt.replace('_', ' ').toUpperCase()}: ${stateStr}`);
};

window.applySearchPreset = function(queryStr) {
  window.closeMenus();
  $('q').value = queryStr;
  addSearchHistory(queryStr);
  const hist = $('searchHistory');
  if (hist) hist.classList.remove('show');
  $('q').focus();
  triggerSearch(true);
  window.showToast(`Applied preset: ${queryStr}`);
};

window.showSyntaxModal = function(show = true) {
  window.closeMenus();
  const m = $('syntaxModal');
  if (m) m.classList.toggle('show', show);
};

// Toast Notifications
let toastTimer = null;
window.showToast = function(msg) {
  const t = $('toast');
  if (!t) return;
  t.textContent = msg;
  t.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    t.classList.remove('show');
  }, 2800);
};

window.toast = function(payload) {
  const msg = typeof payload === 'string' ? payload : (payload?.message || '');
  if (msg) window.showToast(msg);
};

// Zoom adjustment
let currentZoom = 1.0;
window.adjustZoom = function(delta) {
  currentZoom = Math.min(1.8, Math.max(0.7, currentZoom + delta));
  document.documentElement.style.zoom = currentZoom;
  window.showToast(`Zoom: ${Math.round(currentZoom * 100)}%`);
};
window.resetZoom = function() {
  currentZoom = 1.0;
  document.documentElement.style.zoom = '1.0';
  window.showToast('Zoom reset to 100%');
};

// Unified Menu Action Dispatcher
window.menuAction = function(action) {
  window.closeMenus();
  const selected = grid.items[grid.selected] || grid.items[0];

  switch (action) {
    case 'open':
      if (selected) {
        window.ipc.postMessage({ action: 'open', path: selected.Path });
      }
      break;

    case 'folder':
      if (selected) {
        window.ipc.postMessage({ action: 'folder', path: selected.Path });
      }
      break;

    case 'terminal':
      if (selected) {
        window.ipc.postMessage({ action: 'terminal', path: selected.Path });
        window.showToast('Opening PowerShell terminal...');
      } else {
        window.ipc.postMessage({ action: 'terminal', path: '' });
      }
      break;

    case 'run_admin':
      if (selected) {
        window.ipc.postMessage({ action: 'run_admin', path: selected.Path });
        window.showToast(`Launching ${selected.Name} as Administrator`);
      }
      break;

    case 'copy_path':
      if (selected) {
        window.ipc.postMessage({ action: 'copy', path: selected.Path });
        window.showToast('Copied full path to clipboard');
      }
      break;

    case 'copy_name':
      if (selected) {
        window.ipc.postMessage({ action: 'copy_name', name: selected.Name });
        window.showToast('Copied filename to clipboard');
      }
      break;

    case 'copy_dir':
      if (selected) {
        window.ipc.postMessage({ action: 'copy', path: selected.Dir });
        window.showToast('Copied directory path to clipboard');
      }
      break;

    case 'copy_hash':
      if (selected) {
        window.showToast('Calculating SHA-256 hash...');
        window.ipc.postMessage({ action: 'copy_hash', path: selected.Path });
      }
      break;

    case 'export_csv':
      window.showToast('Exporting results to CSV on Desktop...');
      window.ipc.postMessage({
        action: 'export_csv',
        query: $('q').value,
        kind: currentKind,
        ext: currentExt
      });
      break;

    case 'rebuild':
      window.ipc.postMessage({ action: 'rebuild' });
      break;

    case 'properties':
      if (selected) {
        window.ipc.postMessage({ action: 'properties', path: selected.Path });
      }
      break;

    case 'exit':
      window.ipc.postMessage({ action: 'exit_app' });
      break;

    case 'select_all':
      grid.selectedSet.clear();
      for (let i = 0; i < grid.items.length; i++) grid.selectedSet.add(i);
      grid.selected = grid.items.length ? 0 : -1;
      grid.render();
      window.showToast(`Selected all ${grid.items.length} items`);
      break;

    case 'invert_selection':
      if (grid.items.length) {
        for (let i = 0; i < grid.items.length; i++) {
          if (grid.selectedSet.has(i)) grid.selectedSet.delete(i);
          else grid.selectedSet.add(i);
        }
        grid.selected = grid.selectedSet.size ? grid.items.length - 1 : -1;
        grid.render();
        window.showToast(`Selection inverted (${grid.selectedSet.size} selected)`);
      }
      break;

    case 'find':
      $('q').focus();
      $('q').select();
      break;

    case 'clear_search':
      window.clearSearch();
      break;

    case 'zoom_in':
      window.adjustZoom(0.1);
      break;

    case 'zoom_out':
      window.adjustZoom(-0.1);
      break;

    case 'zoom_reset':
      window.resetZoom();
      break;

    case 'refresh':
      triggerSearch(false);
      window.showToast('Refreshed search results');
      break;
  }
};

$('q').addEventListener('input', () => {
  if (!$('q').value.trim()) {
    updateAutocomplete([]);
  }
  triggerSearch(true);
});

// Search history dropdown toggle
$('q').addEventListener('focus', () => {
  const hist = $('searchHistory');
  if (hist && !$('q').value.trim()) {
    renderSearchHistory();
    hist.classList.add('show');
  }
});
$('q').addEventListener('blur', () => {
  setTimeout(() => {
    const hist = $('searchHistory');
    if (hist) hist.classList.remove('show');
  }, 150);
});
if ($('searchHistory')) {
  $('searchHistory').addEventListener('mousedown', (e) => e.preventDefault());
}

if ($('ghostSuggestion')) {
  $('ghostSuggestion').addEventListener('click', () => {
    applyGhostSuggestion();
    $('q').focus();
  });
}

document.addEventListener('keydown', (e) => {
  const inQ = document.activeElement === $('q');

  if (inQ && (e.key === 'Tab' || (e.key === 'ArrowRight' && $('q').selectionStart === $('q').value.length))) {
    if (currentGhostSuggestion && currentGhostSuggestion !== $('q').value) {
      e.preventDefault();
      applyGhostSuggestion();
      return;
    }
  }

  // Help / Cheat sheet shortcut
  if (e.key === 'F1') {
    window.showSyntaxModal(true);
    e.preventDefault();
    return;
  }

  if (e.key === 'F5' || (e.ctrlKey && e.key.toLowerCase() === 'r')) {
    e.preventDefault();
    window.ipc.postMessage({ action: 'rebuild' });
    return;
  }

  // Ctrl+Shift+T: Open Terminal
  if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 't') {
    window.menuAction('terminal');
    e.preventDefault();
    return;
  }

  // Ctrl+S: Export CSV
  if (e.ctrlKey && !e.shiftKey && e.key.toLowerCase() === 's') {
    window.menuAction('export_csv');
    e.preventDefault();
    return;
  }

  // Alt+Enter: Properties
  if (e.altKey && e.key === 'Enter') {
    window.menuAction('properties');
    e.preventDefault();
    return;
  }

  // Ctrl+Shift+1, 2, 3: Layout View Switcher
  if (e.ctrlKey && e.shiftKey && (e.key === '1' || e.key === '!')) { window.setViewMode('list'); e.preventDefault(); return; }
  if (e.ctrlKey && e.shiftKey && (e.key === '2' || e.key === '@')) { window.setViewMode('card'); e.preventDefault(); return; }
  if (e.ctrlKey && e.shiftKey && (e.key === '3' || e.key === '#')) { window.setViewMode('compact'); e.preventDefault(); return; }

  // Ctrl+1, Ctrl+2, Ctrl+3: Sort
  if (e.ctrlKey && !e.shiftKey && e.key === '1') { window.menuSort('name'); e.preventDefault(); return; }
  if (e.ctrlKey && !e.shiftKey && e.key === '2') { window.menuSort('path'); e.preventDefault(); return; }
  if (e.ctrlKey && !e.shiftKey && e.key === '3') { window.menuSort('type'); e.preventDefault(); return; }

  // Alt+1 to Alt+8: Filter presets
  if (e.altKey && e.key === '1') { window.setFilter('all'); e.preventDefault(); return; }
  if (e.altKey && e.key === '2') { window.setFilter('video'); e.preventDefault(); return; }
  if (e.altKey && e.key === '3') { window.setFilter('audio'); e.preventDefault(); return; }
  if (e.altKey && e.key === '4') { window.setFilter('doc'); e.preventDefault(); return; }
  if (e.altKey && e.key === '5') { window.setFilter('image'); e.preventDefault(); return; }
  if (e.altKey && e.key === '6') { window.setFilter('exe'); e.preventDefault(); return; }
  if (e.altKey && e.key === '7') { window.setFilter('archive'); e.preventDefault(); return; }
  if (e.altKey && e.key === '8') { window.setFilter('folders'); e.preventDefault(); return; }

  // Search options
  if (e.ctrlKey && e.key.toLowerCase() === 'i') { window.toggleSearchOption('match_case'); e.preventDefault(); return; }
  if (e.ctrlKey && e.key.toLowerCase() === 'b') { window.toggleSearchOption('whole_word'); e.preventDefault(); return; }
  if (e.ctrlKey && e.key.toLowerCase() === 'u') { window.toggleSearchOption('match_path'); e.preventDefault(); return; }
  if (e.ctrlKey && e.key.toLowerCase() === 'e') { window.toggleSearchOption('regex'); e.preventDefault(); return; }

  // Zoom shortcuts
  if (e.ctrlKey && (e.key === '=' || e.key === '+')) { window.adjustZoom(0.1); e.preventDefault(); return; }
  if (e.ctrlKey && e.key === '-') { window.adjustZoom(-0.1); e.preventDefault(); return; }
  if (e.ctrlKey && e.key === '0') { window.resetZoom(); e.preventDefault(); return; }

  if (e.ctrlKey && ['k', 'f', 'l'].includes(e.key.toLowerCase())) {
    $('q').focus();
    $('q').select();
    e.preventDefault();
    return;
  }

  if (e.altKey && e.key.toLowerCase() === 'p') {
    window.togglePreview();
    e.preventDefault();
    return;
  }

  if (e.altKey && e.key.toLowerCase() === 'c') {
    window.toggleChangelog();
    e.preventDefault();
    return;
  }

  if (e.altKey && e.key.toLowerCase() === 'u') {
    window.checkUpdate();
    e.preventDefault();
    return;
  }

  if (e.altKey && e.key.toLowerCase() === 'a') {
    window.toggleAbout();
    e.preventDefault();
    return;
  }

  if (e.key === 'Escape') {
    window.closeMenus();
    const syntax = $('syntaxModal');
    if (syntax && syntax.classList.contains('show')) {
      window.showSyntaxModal(false);
      e.preventDefault();
      return;
    }
    const about = $('aboutModal');
    if (about && about.classList.contains('show')) {
      window.toggleAbout(false);
      e.preventDefault();
      return;
    }
    const changelog = $('changelogModal');
    if (changelog && changelog.classList.contains('show')) {
      window.toggleChangelog(false);
      e.preventDefault();
      return;
    }
    const updateModal = $('updateModal');
    if (updateModal && updateModal.classList.contains('show')) {
      window.toggleUpdateModal(false);
      e.preventDefault();
      return;
    }
    window.clearSearch();
    e.preventDefault();
    return;
  }
  if (!inQ && grid.items.length) {
    const isCard = grid.viewMode === 'card';
    const cols = isCard ? Math.max(1, grid.cols) : 1;
    const rowH = grid.rowHeight;
    const pageRows = Math.max(1, Math.floor($('rows').clientHeight / rowH) - 2);

    if (e.key === 'ArrowRight' && isCard) {
      grid.select(Math.min(grid.items.length - 1, grid.selected + 1));
      const cardRow = Math.floor(grid.selected / cols);
      $('rows').scrollTop = Math.max(0, cardRow * rowH - 80);
      e.preventDefault();
      return;
    }

    if (e.key === 'ArrowLeft' && isCard) {
      grid.select(Math.max(0, grid.selected - 1));
      const cardRow = Math.floor(grid.selected / cols);
      $('rows').scrollTop = Math.max(0, cardRow * rowH - 80);
      e.preventDefault();
      return;
    }

    if (e.key === 'ArrowDown') {
      const step = isCard ? cols : 1;
      grid.select(Math.min(grid.items.length - 1, grid.selected + step));
      const cardRow = isCard ? Math.floor(grid.selected / cols) : grid.selected;
      $('rows').scrollTop = Math.max(0, cardRow * rowH - 80);
      e.preventDefault();
      return;
    }

    if (e.key === 'ArrowUp') {
      const step = isCard ? cols : 1;
      grid.select(Math.max(0, grid.selected - step));
      const cardRow = isCard ? Math.floor(grid.selected / cols) : grid.selected;
      $('rows').scrollTop = Math.max(0, cardRow * rowH - 80);
      e.preventDefault();
      return;
    }

    if (e.key === 'Home') {
      grid.select(0);
      $('rows').scrollTop = 0;
      e.preventDefault();
      return;
    }

    if (e.key === 'End') {
      grid.select(grid.items.length - 1);
      $('rows').scrollTop = grid.items.length * rowH;
      e.preventDefault();
      return;
    }

    if (e.key === 'PageUp') {
      grid.select(Math.max(0, grid.selected - pageRows));
      $('rows').scrollTop = Math.max(0, grid.selected * rowH - 80);
      e.preventDefault();
      return;
    }

    if (e.key === 'PageDown') {
      grid.select(Math.min(grid.items.length - 1, grid.selected + pageRows));
      $('rows').scrollTop = Math.max(0, grid.selected * rowH - 80);
      e.preventDefault();
      return;
    }
  }

  if (e.key === 'Enter') {
    if (searchTimedOut) {
      searchTimedOut = false;
      triggerSearch(true);
      e.preventDefault();
      return;
    }
    if (inQ) {
      addSearchHistory($('q').value);
    }
    if (inQ && grid.items.length) {
      window.ipc.postMessage({ action: 'open', path: grid.items[0].Path });
    } else if (grid.items[grid.selected]) {
      window.ipc.postMessage({ action: 'open', path: grid.items[grid.selected].Path });
    }
    e.preventDefault();
    return;
  }

  if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'c' && grid.items[grid.selected]) {
    window.ipc.postMessage({ action: 'copy', path: grid.items[grid.selected].Path });
    e.preventDefault();
    return;
  }
});

