export type GridCellAlign = 'left' | 'center' | 'right';

export interface GridColumnDef<T = any> {
  /** Property key or identifier on the row object */
  key: string;

  /** Header title displayed in the table */
  header: string;

  /** Whether the column is currently visible in the grid (defaults to true) */
  visible?: boolean;

  /** Whether the column can be clicked to sort (defaults to true) */
  sortable?: boolean;

  /** Whether the column is included in the global search filter (defaults to true) */
  searchable?: boolean;

  /** Text alignment in header and cell */
  align?: GridCellAlign;

  /** Explicit width (e.g. '120px', '25%') */
  width?: string;

  /** Custom formatter to convert row data to exportable/display string */
  formatter?: (row: T, value: any) => string;

  /** Whether this column should be included in CSV/Excel/Print exports (defaults to true) */
  exportable?: boolean;

  /** If true, the user cannot hide this column (e.g. primary Name/Code) */
  locked?: boolean;

  /** Priority order for column display */
  order?: number;
}

export interface GridSortState {
  column: string;
  direction: 'asc' | 'desc' | null;
}

export type GridExportFormat = 'csv' | 'excel' | 'print';

