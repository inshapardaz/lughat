// A copied "lookup-worthy" selection is a word or short phrase, not a paragraph someone
// happened to copy for an unrelated reason. Kept in its own module (no Electron import) so
// it's testable in plain Node — quick-lookup.ts's own module-scope `import 'electron'`
// can't be loaded outside the actual Electron runtime.
const MAX_CLIPBOARD_LOOKUP_LENGTH = 80;

export function isLookupWorthy(text: string): boolean {
  return text.length > 0 && text.length <= MAX_CLIPBOARD_LOOKUP_LENGTH && !text.includes('\n');
}
