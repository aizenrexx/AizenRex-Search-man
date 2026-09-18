// Virtualized table renderer for AizenRex Search-man
export class VirtualGrid {
  constructor(containerId, spacerId, visibleId, rowHeight = 24) {
    this.container = document.getElementById(containerId);
    this.spacer = document.getElementById(spacerId);
    this.visible = document.getElementById(visibleId);
    this.rowHeight = rowHeight;
    this.items = [];
    this.selected = -1;
    this.lastStart = -1;
    this.iconCache = {
      __folder__: 'data:image/svg+xml;base64,' + btoa('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="#e5a50a" d="M1 3.2c0-.66.54-1.2 1.2-1.2h3l1.4 1.6h7.2c.66 0 1.2.54 1.2 1.2v7.8c0 .77-.63 1.4-1.4 1.4H2.4C1.63 14 1 13.37 1 12.6z"/><path fill="#f8c440" d="M1 5h14l-1.2 8H2.2z"/></svg>')
    };
    this.iconPending = new Set();
    this.iconBusy = false;
    this.iconTimer = null;
    this.onSelect = null;
    this.onOpen = null;
    this.onContext = null;
    this.highlightQuery = '';

    this.container.addEventListener('scroll', () => this.queueRender(), { passive: true });
    this.container.addEventListener('wheel', () => this.queueRender(), { passive: true });
    window.addEventListener('resize', () => {
      this.lastStart = -1;
      this.queueRender();
    });
  }

  setData(items, query = '') {
    this.items = (items || []).map(it => ({
      Name: it.Name ?? it.name ?? '',
      Path: it.Path ?? it.path ?? '',
      Ext: it.Ext ?? it.ext ?? '',
      IsFolder: it.IsFolder ?? it.isFolder ?? false
    }));
    this.highlightQuery = query;
    this.selected = this.items.length ? 0 : -1;
    this.container.scrollTop = 0;
    this.lastStart = -1;
    this.render();
  }

  queueRender() {
    requestAnimationFrame(() => this.render());
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
    const total = this.items.length;
    this.spacer.style.height = Math.max(this.container.clientHeight, total * this.rowHeight) + 'px';

    if (!total) {
      this.lastStart = -1;
      this.visible.style.transform = 'translate3d(0,0,0)';
      this.visible.innerHTML = '<div class="empty">No items match your search.</div>';
      return;
    }

    const scrollTop = this.container.scrollTop;
    const start = Math.max(0, Math.floor(scrollTop / this.rowHeight) - 12);
    const count = Math.ceil(this.container.clientHeight / this.rowHeight) + 24;
    const end = Math.min(total, start + count);

    if (start === this.lastStart && this.visible.dataset.end == end && this.visible.dataset.sel == this.selected) {
      return;
    }

    this.lastStart = start;
    this.visible.dataset.end = end;
    this.visible.dataset.sel = this.selected;
    this.visible.style.transform = `translate3d(0, ${start * this.rowHeight}px, 0)`;
    this.visible.style.height = ((end - start) * this.rowHeight) + 'px';

    let html = '';
    for (let i = start; i < end; i++) {
      const item = this.items[i];
      const isSelected = i === this.selected;
      const extKey = item.IsFolder ? '__folder__' : (item.Ext || '__file__');

      html += `
        <div class="row ${isSelected ? 'selected' : ''}" style="top:${(i - start) * this.rowHeight}px" data-idx="${i}">
          <div>
            <span class="glyph" data-ext="${this.escapeHtml(extKey)}">
              <span class="fallback-icon">${item.IsFolder ? '▰' : '▱'}</span>
            </span>
            ${this.highlight(item.Name)}
            ${i === 0 && this.highlightQuery ? ' <small style="color:#0878d1">Best match</small>' : ''}
          </div>
          <div class="path" title="${this.escapeHtml(item.Path)}">${this.escapeHtml(item.Path)}</div>
          <div class="type">${this.escapeHtml(item.IsFolder ? 'Folder' : (item.Ext ? item.Ext.toUpperCase() : 'File'))}</div>
        </div>
      `;
    }

    this.visible.innerHTML = html;

    // Attach row events
    this.visible.querySelectorAll('.row').forEach(row => {
      const idx = parseInt(row.dataset.idx, 10);
      row.onclick = () => this.select(idx);
      row.ondblclick = () => { if (this.onOpen) this.onOpen(this.items[idx]); };
      row.oncontextmenu = (e) => {
        e.preventDefault();
        this.select(idx);
        if (this.onContext) this.onContext(e, this.items[idx]);
      };
    });

    this.hydrateIcons();
  }

  select(idx) {
    if (idx < 0 || idx >= this.items.length) return;
    this.selected = idx;
    this.render();
    if (this.onSelect) this.onSelect(this.items[idx]);
  }

  hydrateIcons() {
    clearTimeout(this.iconTimer);
    this.iconTimer = setTimeout(() => {
      const reps = {};
      const start = Math.max(0, this.lastStart);
      const end = Math.min(this.items.length, this.lastStart + 80);

      for (let i = start; i < end; i++) {
        const item = this.items[i];
        const key = item.IsFolder ? '__folder__' : (item.Ext || '__file__');
        if (!this.iconCache[key] && !this.iconPending.has(key) && !reps[key]) {
          reps[key] = item.Path;
        }
      }

      const paths = Object.values(reps);
      if (this.iconBusy) {
        this.applyIcons();
        return;
      }

      Object.keys(reps).forEach(k => this.iconPending.add(k));
      if (paths.length && window.ipc) {
        this.iconBusy = true;
        window.ipc.postMessage({ action: 'icons', paths });
      }
      this.applyIcons();
    }, 40);
  }

  receiveIcons(map) {
    this.iconBusy = false;
    Object.keys(map).forEach(k => this.iconPending.delete(k));
    Object.assign(this.iconCache, map);
    this.applyIcons();
    this.hydrateIcons();
  }

  applyIcons() {
    this.visible.querySelectorAll('.glyph[data-ext]').forEach(el => {
      const src = this.iconCache[el.dataset.ext];
      if (src) el.innerHTML = `<img src="${src}">`;
    });
  }
}
