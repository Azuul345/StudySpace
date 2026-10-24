import type { ComponentType } from 'react'

/// A diagram is code, not content: packs only name it by id (REQ-VIS-001).
export interface DiagramDef {
  title: string
  /// Shown under the picture and read by screen readers.
  description: string
  viewBox: string
  Body: ComponentType
}
