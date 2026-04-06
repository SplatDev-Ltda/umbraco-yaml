// Schema2Yaml Dashboard — Lit element for Umbraco 14–17
// Registered as a custom element; referenced from umbraco-package.json.
// Uses UMB_AUTH_CONTEXT for authenticated API calls.

import { LitElement, html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';

const API_BASE = '/umbraco/api/SchemaExport';

class Schema2YamlDashboard extends UmbElementMixin(LitElement) {

    static properties = {
        _loading:          { state: true },
        _downloadingZip:   { state: true },
        _stats:            { state: true },
        _yaml:             { state: true },
        _yamlPreview:      { state: true },
        _previewTruncated: { state: true },
        _hasExport:        { state: true },
    };

    static styles = css`
        :host {
            display: block;
            padding: var(--uui-size-layout-1, 24px);
        }

        .header {
            margin-bottom: var(--uui-size-layout-2, 32px);
        }

        .header h1 {
            font-size: var(--uui-type-h3-size, 1.5rem);
            font-weight: 600;
            margin: 0 0 var(--uui-size-3, 8px) 0;
            color: var(--uui-color-text, #1b264f);
        }

        .header p {
            margin: 0;
            color: var(--uui-color-text-alt, #666);
        }

        .actions {
            display: flex;
            gap: var(--uui-size-3, 8px);
            flex-wrap: wrap;
            margin-bottom: var(--uui-size-layout-2, 32px);
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
            gap: var(--uui-size-3, 8px);
            margin-bottom: var(--uui-size-layout-2, 32px);
        }

        .stat-card {
            background: var(--uui-color-surface, #fff);
            border: 1px solid var(--uui-color-border, #e3e3e3);
            border-radius: var(--uui-border-radius, 4px);
            padding: var(--uui-size-5, 16px);
            text-align: center;
        }

        .stat-label {
            font-size: 11px;
            color: var(--uui-color-text-alt, #888);
            text-transform: uppercase;
            letter-spacing: 0.05em;
            margin-bottom: var(--uui-size-2, 6px);
        }

        .stat-value {
            font-size: 28px;
            font-weight: 600;
            color: var(--uui-color-interactive, #1b264f);
            line-height: 1;
        }

        .stat-meta {
            font-size: 11px;
            color: var(--uui-color-text-alt, #888);
            margin-top: var(--uui-size-2, 6px);
        }

        .preview-box {
            margin-top: var(--uui-size-layout-2, 32px);
        }

        .preview-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: var(--uui-size-3, 8px);
        }

        .preview-header h3 {
            margin: 0;
            font-size: var(--uui-type-h5-size, 1rem);
            font-weight: 600;
        }

        .truncated-note {
            font-size: 12px;
            font-style: italic;
            color: var(--uui-color-warning-emphasis, #a0522d);
        }

        pre.yaml {
            background: var(--uui-color-surface-alt, #f5f5f5);
            border: 1px solid var(--uui-color-border, #e3e3e3);
            border-radius: var(--uui-border-radius, 4px);
            padding: var(--uui-size-5, 16px);
            font-family: 'Courier New', Consolas, monospace;
            font-size: 12px;
            line-height: 1.6;
            max-height: 520px;
            overflow: auto;
            white-space: pre;
            margin: 0;
        }
    `;

    constructor() {
        super();
        this._loading = false;
        this._downloadingZip = false;
        this._stats = null;
        this._yaml = null;
        this._yamlPreview = null;
        this._previewTruncated = false;
        this._hasExport = false;
        this._authContext = null;
        this._notificationContext = null;
    }

    connectedCallback() {
        super.connectedCallback();

        // Resolve auth context for authenticated fetches
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
            this._authContext = ctx;
            // Load last-export statistics on mount (silently — may not exist yet)
            this._loadStatistics();
        });

        // Resolve notification context for toast messages
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (ctx) => {
            this._notificationContext = ctx;
        });
    }

    // ─── Auth helper ───────────────────────────────────────────────────────────

    async _fetchAuthenticated(path, options = {}) {
        const headers = { 'Content-Type': 'application/json', ...(options.headers ?? {}) };

        if (this._authContext) {
            const token = await this._authContext.getLatestToken();
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }

        return fetch(`${API_BASE}${path}`, { ...options, headers });
    }

    // ─── API calls ─────────────────────────────────────────────────────────────

    async _loadStatistics() {
        try {
            const res = await this._fetchAuthenticated('/Statistics');
            if (res.ok) {
                const data = await res.json();
                if (data && data.dataTypes !== undefined) {
                    this._stats = data;
                }
            }
        } catch {
            // No prior export — silently ignore
        }
    }

    async _runExport() {
        if (this._loading) return;

        this._loading = true;
        this._hasExport = false;
        this._stats = null;
        this._yaml = null;
        this._yamlPreview = null;

        try {
            const res = await this._fetchAuthenticated('/Export');

            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: res.statusText }));
                throw new Error(err.message ?? res.statusText);
            }

            const data = await res.json();
            this._stats = data.statistics;
            this._applyYaml(data.yaml);
            this._notify('positive', 'Export complete', 'Schema exported successfully.');
        } catch (e) {
            this._notify('danger', 'Export failed', e.message ?? 'An unexpected error occurred.');
        } finally {
            this._loading = false;
        }
    }

    _applyYaml(yaml) {
        const LIMIT = 10000;
        this._yaml = yaml;
        this._hasExport = true;

        if (yaml && yaml.length > LIMIT) {
            this._yamlPreview = yaml.substring(0, LIMIT);
            this._previewTruncated = true;
        } else {
            this._yamlPreview = yaml;
            this._previewTruncated = false;
        }
    }

    async _downloadYaml() {
        // Re-exports server-side and streams the file
        const url = `${API_BASE}/DownloadYaml`;
        await this._triggerDownload(url);
    }

    async _downloadZip() {
        if (this._downloadingZip) return;
        this._downloadingZip = true;

        try {
            const url = `${API_BASE}/DownloadZip`;
            await this._triggerDownload(url);
        } finally {
            // Small delay so the button state is visible before re-enabling
            setTimeout(() => { this._downloadingZip = false; }, 2000);
        }
    }

    async _triggerDownload(url) {
        // Use authenticated fetch → blob → object URL download
        try {
            const res = await this._fetchAuthenticated(url.replace(API_BASE, ''));

            if (!res.ok) {
                throw new Error(`Server returned ${res.status}: ${res.statusText}`);
            }

            const blob = await res.blob();
            const objectUrl = URL.createObjectURL(blob);
            const a = document.createElement('a');

            // Derive filename from Content-Disposition header if present
            const cd = res.headers.get('Content-Disposition') ?? '';
            const match = cd.match(/filename[^;=\n]*=["']?([^"';\n]+)/i);
            a.download = match ? match[1].trim() : 'umbraco-export';
            a.href = objectUrl;

            // Prevent the History API / anchor error in Umbraco 14+ by NOT appending to document body
            // A simple click() on the detached anchor element triggers the download in modern browsers
            a.click();

            setTimeout(() => URL.revokeObjectURL(objectUrl), 100);
        } catch (e) {
            this._notify('danger', 'Download failed', e.message ?? 'Could not download file.');
        }
    }

    // ─── Notifications ─────────────────────────────────────────────────────────

    _notify(color, headline, message) {
        if (this._notificationContext) {
            this._notificationContext.peek(color, { data: { headline, message } });
        }
    }

    // ─── Render ────────────────────────────────────────────────────────────────

    _renderStats() {
        if (!this._stats) return nothing;

        const s = this._stats;

        return html`
            <div class="stats-grid">
                ${this._statCard('Languages',       s.languages)}
                ${this._statCard('Data Types',      s.dataTypes)}
                ${this._statCard('Document Types',  s.documentTypes)}
                ${this._statCard('Media Types',     s.mediaTypes)}
                ${this._statCard('Templates',       s.templates)}
                ${this._statCard('Content Nodes',   s.content)}
                ${this._statCard('Media Items',     s.media)}
                ${this._statCard('Dictionary Items',s.dictionaryItems)}
                ${this._statCard('Members',         s.members)}
                ${this._statCard('Users',           s.users)}
            </div>
            ${s.umbracoVersion ? html`
                <p class="stat-meta">
                    Exported ${s.exportDate ? new Date(s.exportDate).toLocaleString() : ''}
                    &mdash; Umbraco ${s.umbracoVersion}
                    ${s.durationSeconds != null ? html`&mdash; ${s.durationSeconds.toFixed(2)}s` : nothing}
                </p>` : nothing}
        `;
    }

    _statCard(label, value) {
        return html`
            <div class="stat-card">
                <div class="stat-label">${label}</div>
                <div class="stat-value">${value ?? 0}</div>
            </div>`;
    }

    _renderPreview() {
        if (!this._yamlPreview) return nothing;

        return html`
            <div class="preview-box">
                <div class="preview-header">
                    <h3>YAML Preview</h3>
                    ${this._previewTruncated
                        ? html`<span class="truncated-note">Showing first 10 000 chars — download for the full export</span>`
                        : nothing}
                </div>
                <pre class="yaml">${this._yamlPreview}${this._previewTruncated ? '\n… (truncated)' : ''}</pre>
            </div>`;
    }

    render() {
        return html`
            <div class="header">
                <h1>Schema Export</h1>
                <p>Export your Umbraco site structure to YAML for version control and migration.</p>
            </div>

            <div class="actions">
                <uui-button
                    look="primary"
                    color="default"
                    label=${this._loading ? 'Exporting…' : 'Export to YAML'}
                    ?disabled=${this._loading}
                    @click=${this._runExport}>
                    ${this._loading ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
                    ${this._loading ? 'Exporting…' : 'Export to YAML'}
                </uui-button>

                <uui-button
                    look="secondary"
                    color="default"
                    label="Download YAML"
                    ?disabled=${!this._hasExport || this._loading}
                    @click=${this._downloadYaml}>
                    Download YAML
                </uui-button>

                <uui-button
                    look="secondary"
                    color="default"
                    label=${this._downloadingZip ? 'Preparing ZIP…' : 'Download ZIP (with media)'}
                    ?disabled=${this._downloadingZip || !this._hasExport}
                    @click=${this._downloadZip}>
                    ${this._downloadingZip ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
                    ${this._downloadingZip ? 'Preparing ZIP…' : 'Download ZIP (with media)'}
                </uui-button>
            </div>

            ${this._renderStats()}
            ${this._renderPreview()}
        `;
    }
}

customElements.define('schema2yaml-dashboard', Schema2YamlDashboard);

export default Schema2YamlDashboard;
