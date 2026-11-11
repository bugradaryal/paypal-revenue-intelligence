// Fails when user-visible text is hard-coded in a .tsx file instead of coming from src/i18n.
// Checks: JSX text, string literals in text-like JSX attributes, and string literals used as JSX children.
// Escape hatch for genuinely literal text (brand names, code samples): put "i18n-ignore" in a comment
// on the same line or the line above.
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { parse } from '@babel/parser'

const ROOT = fileURLToPath(new URL('../src', import.meta.url))
const SKIP_DIRS = new Set([join(ROOT, 'i18n')])
const TEXT_ATTRS = new Set([
  'aria-label', 'aria-description', 'placeholder', 'title', 'alt', 'label', 'description', 'hint', 'error',
  'content', 'closeLabel', 'dismissLabel', 'summary', 'tooltip', 'helperText',
])
const hasLetters = (s) => /\p{L}/u.test(s)

function* walkFiles(dir) {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) {
      if (!SKIP_DIRS.has(path)) yield* walkFiles(path)
    } else if (path.endsWith('.tsx')) yield path
  }
}

function* children(node) {
  for (const [key, value] of Object.entries(node)) {
    if (key === 'loc' || key === 'start' || key === 'end' || key === 'leadingComments' || key === 'trailingComments') continue
    if (Array.isArray(value)) {
      for (const v of value) {
        if (v && typeof v.type === 'string') yield v
      }
    } else if (value && typeof value.type === 'string') {
      yield value
    }
  }
}

const problems = []

for (const file of walkFiles(ROOT)) {
  const source = readFileSync(file, 'utf8')
  const lines = source.split('\n')
  const ignored = (line) => /i18n-ignore/.test(lines[line - 1] ?? '') || /i18n-ignore/.test(lines[line - 2] ?? '')
  const ast = parse(source, { sourceType: 'module', plugins: ['typescript', 'jsx'] })

  const report = (node, text) => {
    const { line, column } = node.loc.start
    if (!ignored(line)) problems.push(`${relative(join(ROOT, '..'), file).split(sep).join('/')}:${line}:${column + 1}  "${text.trim().replace(/\s+/g, ' ').slice(0, 60)}"`)
  }

  const visit = (node, parent) => {
    if (node.type === 'JSXText' && hasLetters(node.value)) report(node, node.value)

    if (node.type === 'JSXAttribute' && node.name.type === 'JSXIdentifier' && TEXT_ATTRS.has(node.name.name) && node.value) {
      const v = node.value
      if (v.type === 'StringLiteral' && hasLetters(v.value)) report(v, v.value)
      if (v.type === 'JSXExpressionContainer') {
        const e = v.expression
        if (e.type === 'StringLiteral' && hasLetters(e.value)) report(e, e.value)
        if (e.type === 'TemplateLiteral' && e.expressions.length === 0 && hasLetters(e.quasis[0].value.cooked ?? '')) report(e, e.quasis[0].value.cooked)
      }
    }

    // {'some text'} used as a child
    if (node.type === 'JSXExpressionContainer' && parent && (parent.type === 'JSXElement' || parent.type === 'JSXFragment')) {
      if (node.expression.type === 'StringLiteral' && hasLetters(node.expression.value)) report(node, node.expression.value)
    }

    for (const child of children(node)) visit(child, node)
  }
  visit(ast.program, null)
}

if (problems.length) {
  console.error(`i18n check failed: ${problems.length} hard-coded text(s). Move them into src/i18n/locales/*.\n`)
  for (const p of problems) console.error('  ' + p)
  process.exit(1)
}
console.log('i18n check passed: no hard-coded UI text.')
