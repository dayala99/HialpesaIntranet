import { DyeingCatalogOption } from './dyeing.models';
export interface QualityControlValues {
  weight: number;
  width: number;
  shrinkWidth: number;
  shrinkLength: number;
  twist: number;
  thirdShrinkWidth: number;
  thirdShrinkLength: number;
  thirdTwist: number;
  washedWeight: number;
  laundryWeight: number;
  elongationLength: number;
  elongationWidth: number;
  rejectedWeight: number;
  plantWidth: number;
  resistance: number;
  residualShrinkWidth: number;
  residualShrinkLength: number;
  residualTwist: number;
  ramaWeight: number;
  ramaWidth: number;
  ramaShrinkWidth: number;
  ramaShrinkLength: number;
  ramaTwist: number;
  ramaWashedWeight: number;
  widthType: string; shrinkType: string; ramaType: string; approval: string;
  observations: string; rejectionReason: string; alternativeFabric: string; skew: string; inclination: string; washType: string;
}
export interface QualityRecommendation { code: string; description: string; selected: boolean; }
export interface QualityReference { weight: number; width: number; shrinkWidth: number; shrinkLength: number; fabricTwist: number; widthType: string; alternativeDescription: string; }
export interface QualityControlDetail { batch: string; sequence: number; fabric: string; combination: string; color: string; description: string; values: QualityControlValues; programmed: QualityReference; modifiedAt: string | null; canEdit: boolean; recommendations: QualityRecommendation[]; }
export interface QualityControlScreen { detail: QualityControlDetail; catalogs: DyeingCatalogOption[]; }
export interface QualityControlUpdate { values: QualityControlValues; expectedModifiedAt: string | null; recommendations: string[]; }
export interface QualityControlDialogData { batch: string; sequence: number; }
export interface QualityControlDialogResult { saved: boolean; }
export type QualityNumberField = 'weight' | 'width' | 'shrinkWidth' | 'shrinkLength' | 'twist' | 'thirdShrinkWidth' | 'thirdShrinkLength' | 'thirdTwist' | 'washedWeight' | 'laundryWeight' | 'elongationLength' | 'elongationWidth' | 'rejectedWeight' | 'plantWidth' | 'resistance' | 'residualShrinkWidth' | 'residualShrinkLength' | 'residualTwist' | 'ramaWeight' | 'ramaWidth' | 'ramaShrinkWidth' | 'ramaShrinkLength' | 'ramaTwist' | 'ramaWashedWeight';
export interface QualityField { key: QualityNumberField; label: string; section: string; min: number; max: number; step: number; }
export const QUALITY_FIELDS: QualityField[] = [
  {
    "key": "weight",
    "label": "Gramaje (g/m²)",
    "section": "final",
    "min": 0,
    "max": 9999,
    "step": 1
  },
  {
    "key": "width",
    "label": "Ancho (m)",
    "section": "final",
    "min": 0,
    "max": 99.999,
    "step": 0.001
  },
  {
    "key": "shrinkWidth",
    "label": "Encogimiento ancho",
    "section": "final",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "shrinkLength",
    "label": "Encogimiento largo",
    "section": "final",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "twist",
    "label": "Revirado",
    "section": "final",
    "min": -999.99,
    "max": 999.99,
    "step": 0.01
  },
  {
    "key": "thirdShrinkWidth",
    "label": "Encog. ancho 3ra. lavada",
    "section": "third",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "thirdShrinkLength",
    "label": "Encog. largo 3ra. lavada",
    "section": "third",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "thirdTwist",
    "label": "Revirado 3ra. lavada",
    "section": "third",
    "min": -999.99,
    "max": 999.99,
    "step": 0.01
  },
  {
    "key": "washedWeight",
    "label": "Gramaje lavado",
    "section": "additional",
    "min": 0,
    "max": 9999,
    "step": 1
  },
  {
    "key": "laundryWeight",
    "label": "Gramaje lavandería",
    "section": "additional",
    "min": 0,
    "max": 9999,
    "step": 1
  },
  {
    "key": "elongationLength",
    "label": "Elongación largo",
    "section": "additional",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "elongationWidth",
    "label": "Elongación ancho",
    "section": "additional",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "rejectedWeight",
    "label": "Kilos rechazados",
    "section": "rejection",
    "min": 0,
    "max": 99999999999.99,
    "step": 0.01
  },
  {
    "key": "plantWidth",
    "label": "Ancho planta",
    "section": "additional",
    "min": 0,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "resistance",
    "label": "Resistencia tela",
    "section": "additional",
    "min": 0,
    "max": 999.99,
    "step": 0.01
  },
  {
    "key": "residualShrinkWidth",
    "label": "Encogimiento ancho",
    "section": "residual",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "residualShrinkLength",
    "label": "Encogimiento largo",
    "section": "residual",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "residualTwist",
    "label": "Revirado",
    "section": "residual",
    "min": -999.99,
    "max": 999.99,
    "step": 0.01
  },
  {
    "key": "ramaWeight",
    "label": "Gramaje (g/m²)",
    "section": "rama",
    "min": 0,
    "max": 9999,
    "step": 1
  },
  {
    "key": "ramaWidth",
    "label": "Ancho (m)",
    "section": "rama",
    "min": 0,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "ramaShrinkWidth",
    "label": "Encogimiento ancho",
    "section": "rama",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "ramaShrinkLength",
    "label": "Encogimiento largo",
    "section": "rama",
    "min": -9999.99,
    "max": 9999.99,
    "step": 0.01
  },
  {
    "key": "ramaTwist",
    "label": "Revirado",
    "section": "rama",
    "min": -999.99,
    "max": 999.99,
    "step": 0.01
  },
  {
    "key": "ramaWashedWeight",
    "label": "Gramaje lavado",
    "section": "rama",
    "min": 0,
    "max": 9999,
    "step": 1
  }
];
