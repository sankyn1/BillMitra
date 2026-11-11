import { Injectable, inject } from '@angular/core';
import { GridColumnDef } from '../models/grid-column.model';
import { DialogService } from './dialog.service';

@Injectable({
  providedIn: 'root'
})
export class GridExportService {
  private dialogService = inject(DialogService);

  /**
   * Export grid records to CSV with UTF-8 BOM support for Excel compatibility.
   */
  exportToCsv<T>(filename: string, columns: GridColumnDef<T>[], rows: T[]): void {
    const exportColumns = columns.filter(c => c.visible !== false && c.exportable !== false);
    if (!exportColumns.length || !rows.length) {
      this.dialogService.toast('No visible data to export.', 'warning');
      return;
    }

    const cleanFilename = this.sanitizeFilename(filename, 'csv');
    const headerRow = exportColumns.map(c => this.escapeCsvValue(c.header)).join(',');

    const dataRows = rows.map(row => {
      return exportColumns.map(col => {
        const val = this.extractCellValue(row, col);
        return this.escapeCsvValue(val);
      }).join(',');
    });

    const csvContent = '\uFEFF' + [headerRow, ...dataRows].join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    this.triggerDownload(blob, cleanFilename);
    this.dialogService.toast(`Exported ${rows.length} records to ${cleanFilename}`, 'success');
  }

  /**
   * Export grid records to Microsoft Excel XML Spreadsheet format (.xls)
   * which preserves columns, numbers, and formatting without requiring external heavy libraries.
   */
  exportToExcel<T>(filename: string, columns: GridColumnDef<T>[], rows: T[], sheetName = 'Data'): void {
    const exportColumns = columns.filter(c => c.visible !== false && c.exportable !== false);
    if (!exportColumns.length || !rows.length) {
      this.dialogService.toast('No visible data to export.', 'warning');
      return;
    }

    const cleanFilename = this.sanitizeFilename(filename, 'xls');

    let xml = `<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
  xmlns:o="urn:schemas-microsoft-com:office:office"
  xmlns:x="urn:schemas-microsoft-com:office:excel"
  xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"
  xmlns:html="http://www.w3.org/TR/REC-html40">
  <Styles>
    <Style ss:ID="Header">
      <Font ss:Bold="1" ss:Color="#FFFFFF"/>
      <Interior ss:Color="#1E3A8A" ss:Pattern="Solid"/>
      <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
      <Borders>
        <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#0F172A"/>
      </Borders>
    </Style>
    <Style ss:ID="RowEven">
      <Interior ss:Color="#FFFFFF" ss:Pattern="Solid"/>
      <Borders>
        <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
      </Borders>
    </Style>
    <Style ss:ID="RowOdd">
      <Interior ss:Color="#F8FAFC" ss:Pattern="Solid"/>
      <Borders>
        <Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E2E8F0"/>
      </Borders>
    </Style>
    <Style ss:ID="NumberCell">
      <Alignment ss:Horizontal="Right"/>
    </Style>
  </Styles>
  <Worksheet ss:Name="${this.escapeXml(sheetName)}">
    <Table>
`;

    // Column widths
    exportColumns.forEach(() => {
      xml += `      <Column ss:AutoFitWidth="1" ss:Width="120"/>\n`;
    });

    // Header Row
    xml += `      <Row ss:Height="24">\n`;
    exportColumns.forEach(c => {
      xml += `        <Cell ss:StyleID="Header"><Data ss:Type="String">${this.escapeXml(c.header)}</Data></Cell>\n`;
    });
    xml += `      </Row>\n`;

    // Data Rows
    rows.forEach((row, idx) => {
      const styleId = idx % 2 === 0 ? 'RowEven' : 'RowOdd';
      xml += `      <Row ss:Height="20">\n`;
      exportColumns.forEach(c => {
        const rawVal = this.extractCellValue(row, c);
        const isNumeric = typeof rawVal === 'number' || (!isNaN(Number(rawVal)) && rawVal !== '' && !String(rawVal).startsWith('0') && String(rawVal).trim().length > 0 && !String(rawVal).includes('-') && !String(rawVal).includes('/'));
        const dataType = isNumeric ? 'Number' : 'String';
        const strVal = isNumeric ? Number(rawVal) : this.escapeXml(String(rawVal ?? ''));

        xml += `        <Cell ss:StyleID="${styleId}"><Data ss:Type="${dataType}">${strVal}</Data></Cell>\n`;
      });
      xml += `      </Row>\n`;
    });

    xml += `    </Table>
  </Worksheet>
</Workbook>`;

    const blob = new Blob([xml], { type: 'application/vnd.ms-excel;charset=utf-8' });
    this.triggerDownload(blob, cleanFilename);
    this.dialogService.toast(`Exported ${rows.length} records to ${cleanFilename}`, 'success');
  }

  /**
   * Print visible grid records in a printer-optimized format.
   */
  printGrid<T>(title: string, columns: GridColumnDef<T>[], rows: T[]): void {
    const exportColumns = columns.filter(c => c.visible !== false && c.exportable !== false);
    if (!exportColumns.length || !rows.length) {
      this.dialogService.toast('No visible data to print.', 'warning');
      return;
    }

    const printWindow = window.open('', '_blank');
    if (!printWindow) {
      this.dialogService.alert('Popup Blocked', 'Please allow popups to print data tables.', 'warning');
      return;
    }

    const now = new Date().toLocaleString();
    let tableHeadersHtml = exportColumns.map(c => `
      <th style="padding: 8px 12px; text-align: ${c.align || 'left'}; border: 1px solid #cbd5e1; background: #1e3a8a; color: #fff; font-size: 11px; text-transform: uppercase;">
        ${c.header}
      </th>
    `).join('');

    let tableRowsHtml = rows.map((row, i) => {
      const bg = i % 2 === 0 ? '#ffffff' : '#f8fafc';
      const cells = exportColumns.map(c => `
        <td style="padding: 6px 12px; text-align: ${c.align || 'left'}; border: 1px solid #e2e8f0; font-size: 12px;">
          ${this.extractCellValue(row, c)}
        </td>
      `).join('');
      return `<tr style="background: ${bg};">${cells}</tr>`;
    }).join('');

    const html = `
      <!DOCTYPE html>
      <html>
        <head>
          <title>${title} - BillMitra Print</title>
          <style>
            body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Arial, sans-serif; margin: 20px; color: #0f172a; }
            .header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 16px; border-bottom: 2px solid #1e3a8a; padding-bottom: 10px; }
            .title { font-size: 20px; font-weight: bold; color: #1e3a8a; margin: 0; }
            .meta { font-size: 11px; color: #64748b; }
            table { width: 100%; border-collapse: collapse; font-size: 12px; }
            @media print {
              body { margin: 0; }
              @page { size: landscape; margin: 10mm; }
            }
          </style>
        </head>
        <body>
          <div class="header">
            <div>
              <div class="title">BillMitra - ${title}</div>
              <div class="meta">Exported on: ${now} | Total Records: ${rows.length}</div>
            </div>
          </div>
          <table>
            <thead><tr>${tableHeadersHtml}</tr></thead>
            <tbody>${tableRowsHtml}</tbody>
          </table>
          <script>
            window.onload = function() {
              window.print();
              setTimeout(function() { window.close(); }, 500);
            };
          </script>
        </body>
      </html>
    `;

    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
  }

  /**
   * Resolves cell value using column formatter or nested property path.
   */
  public extractCellValue<T>(row: T, col: GridColumnDef<T>): any {
    if (!row) return '';
    const rawVal = (row as any)[col.key];
    if (col.formatter) {
      try {
        return col.formatter(row, rawVal);
      } catch {
        return rawVal ?? '';
      }
    }
    if (rawVal === undefined || rawVal === null) return '';
    if (rawVal instanceof Date) return rawVal.toLocaleDateString();
    return rawVal;
  }

  private escapeCsvValue(val: any): string {
    if (val === null || val === undefined) return '""';
    const str = String(val).replace(/"/g, '""');
    return `"${str}"`;
  }

  private escapeXml(unsafe: string): string {
    return unsafe.replace(/[<>&'"]/g, (c) => {
      switch (c) {
        case '<': return '&lt;';
        case '>': return '&gt;';
        case '&': return '&amp;';
        case '\'': return '&apos;';
        case '"': return '&quot;';
        default: return c;
      }
    });
  }

  private sanitizeFilename(name: string, ext: string): string {
    const clean = name.replace(/[^a-z0-9_-]/gi, '_').toLowerCase();
    const ts = new Date().toISOString().slice(0, 10);
    return `${clean}_${ts}.${ext}`;
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }
}
