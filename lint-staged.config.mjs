// lint-staged runs from the repo root; frontend binaries live in frontend/node_modules/.bin/
const eslint = 'frontend/node_modules/.bin/eslint'
const prettier = 'frontend/node_modules/.bin/prettier'

export default {
  'frontend/**/*.{ts,tsx}': [
    `${eslint} --fix --max-warnings=0`,
    `${prettier} --write`,
  ],
  'frontend/**/*.{js,mjs,cjs,json,css,html,md}': [`${prettier} --write`],
  'backend/**/*.cs': ['dotnet format backend/SponsorshipApproval.sln --include'],
}
