/**
 * get-changed-lines.js
 *
 * This script uses Git to detect file and line-level changes between a given base branch and HEAD.
 * It outputs a structured list of objects, each containing a file path and the line ranges that have been added or modified.
 *
 * Example output:
 * [
 *   {
 *     name: "src/Example.cs",
 *     lines: [[10, 12], [20, 20]]
 *   }
 * ]
 *
 * CLI Usage:
 *   node get-changed-lines.js --base-branch=origin/main
 *     (defaults to 'origin/canary' if no branch is provided)
 *
 * Use Cases:
 *   - Filtering static analysis warnings by changed lines only
 *   - CI workflows that post comments only on modified code
 *   - Build/test optimization based on touched files
 */

const { execSync } = require('child_process');

// Parse CLI arguments for --base-branch
let BASE_BRANCH = 'origin/main';
const baseBranchArg = process.argv.find(arg => arg.startsWith('--base-branch='));
if (baseBranchArg) {
  BASE_BRANCH = baseBranchArg.split('=')[1];
}

/**
 * Returns a list of changed files between BASE_BRANCH and HEAD.
 */
function getChangedFiles() {
  const changedFilesOutput = execSync(`git diff --name-only ${BASE_BRANCH}...HEAD`, {
    encoding: 'utf-8'
  });
  return changedFilesOutput.trim().split('\n').filter(Boolean);
}

/**
 * Returns a list of [startLine, endLine] for each changed hunk in a file.
 *
 * @param {string} file - The file to get changed lines for
 * @returns {Array<[number, number]>}
 */
function getLineChanges(file) {
  const fileDiff = execSync(`git diff --unified=0 --diff-filter=AM ${BASE_BRANCH}...HEAD -- ${file}`, {
    encoding: 'utf-8'
  });
  const diffLines = fileDiff.trim().split('\n');
  const lineChanges = [];
  diffLines.forEach(line => {
    // Example hunk header: @@ -10,2 +20,3 @@
    // This means:
    // - Old file: starting at line 10, 2 lines were removed.
    // + New file: starting at line 20, 3 lines were added/changed. In this case, hunkMatch[1] is 20, hunkMatch[2] is 3
    // We're interested in the new file's line range: +20,3 → lines 20–22
    // For more information on meaning of diff output, read here: Read what is means here: https://unix.stackexchange.com/questions/81998/understanding-of-diff-output
    const hunkMatch = line.match(/^@@ -[0-9]+(?:,[0-9]+)? \+([0-9]+)(?:,([0-9]+))? @@/);
    if (hunkMatch) {
      const startLine = parseInt(hunkMatch[1], 10);
      const numLines = hunkMatch[2] ? parseInt(hunkMatch[2], 10) : 1;
      lineChanges.push([startLine, startLine + numLines - 1]);
    }
  });

  return lineChanges;
}

/**
 * Returns a list of objects like:
 *   [
 *     { name: 'file1.cs', lines: [[10,12], [20,20]] },
 *     { name: 'file2.cs', lines: [[5,5]] }
 *   ]
 */
function getChangedFileLineData() {
  const changedFiles = getChangedFiles();
  return changedFiles.map(file => ({
    name: file,
    lines: getLineChanges(file)
  }));
}

module.exports = {
  getChangedFileLineData
};

// If run directly: print output to console
if (require.main === module) {
  const data = getChangedFileLineData();
  console.log(JSON.stringify(data, null, 2));
}
