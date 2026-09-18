// Preview pane controller for AizenRex Search-man
export class PreviewController {
  constructor(paneId, boxId, nameId, typeId, sizeId, pathId) {
    this.pane = document.getElementById(paneId);
    this.box = document.getElementById(boxId);
    this.nameEl = document.getElementById(nameId);
    this.typeEl = document.getElementById(typeId);
    this.sizeEl = document.getElementById(sizeId);
    this.pathEl = document.getElementById(pathId);
  }

  toggle() {
    this.pane.classList.toggle('preview-on');
  }

  isOpen() {
    return this.pane.classList.contains('preview-on');
  }

  show(p) {
    if (!p) {
      this.clear();
      return;
    }

    this.nameEl.textContent = p.Name || 'No selection';
    this.typeEl.textContent = p.Kind || '—';
    this.sizeEl.textContent = p.Size || '—';
    this.pathEl.textContent = p.Path || '—';

    this.box.innerHTML = '';
    if (p.Kind === 'image' && p.Content) {
      const img = document.createElement('img');
      img.src = p.Content;
      this.box.appendChild(img);
    } else if (p.Kind === 'text' && p.Content !== undefined) {
      const pre = document.createElement('pre');
      pre.textContent = p.Content || '(Empty file)';
      this.box.appendChild(pre);
    } else {
      this.box.innerHTML = '<span style="color:#888;">No inline preview available</span>';
    }
  }

  clear() {
    this.nameEl.textContent = 'No selection';
    this.typeEl.textContent = '—';
    this.sizeEl.textContent = '—';
    this.pathEl.textContent = '—';
    this.box.innerHTML = '<span style="color:#888;">Select a file to preview</span>';
  }
}
