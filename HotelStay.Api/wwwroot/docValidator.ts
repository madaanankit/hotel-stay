// Frontend document validator (framework-free TypeScript)
// Keep city lists in sync with server-side DocumentValidator.

export type DocumentType = 'Passport' | 'NationalId' | 'DriverLicense' | 'Other';

const DOMESTIC = new Set(['NYC','LAX']);
const INTERNATIONAL = new Set(['LON','TYO','PAR']);

export function validateDocumentForDestination(destination: string | null | undefined, documentType: DocumentType) {
  if (!destination) {
    return { isValid: false, code: 'UnknownDestination', field: 'destination', message: 'Destination is required' };
  }

  const dest = destination.trim().toUpperCase();

  if (DOMESTIC.has(dest)) {
    if (documentType === 'DriverLicense' || documentType === 'NationalId' || documentType === 'Passport')
      return { isValid: true };

    return { isValid: false, code: 'InvalidDocument', field: 'documentType', message: 'For domestic destinations a DriverLicense, NationalId or Passport is required' };
  }

  if (INTERNATIONAL.has(dest)) {
    if (documentType === 'Passport') return { isValid: true };
    return { isValid: false, code: 'PassportRequired', field: 'documentType', message: 'A passport is required for international destinations' };
  }

  // Unknown destination: require passport by default
  if (documentType === 'Passport') return { isValid: true };
  return { isValid: false, code: 'UnknownDestinationPassportRequired', field: 'documentType', message: 'Passport is required for this destination' };
}
