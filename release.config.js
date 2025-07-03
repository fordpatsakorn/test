module.exports = {
  branches: ['canary'],
  tagFormat: 'release/${version}',
  plugins: [
    '@semantic-release/commit-analyzer',
    {
      releaseRules: [
        { type: 'feat', release: 'minor' },
        { type: 'maintenance', release: 'patch' },
        { type: 'refactor', release: 'patch' },
        { type: 'fix', release: 'patch' },
        { type: 'config', release: 'patch' },
        { type: 'infra', release: 'patch' },
        { type: 'chore', release: false },
      ]
    },
    '@semantic-release/github',
    [
      'semantic-release-jira-notes',
      {
        jiraHost: 'flowaccount.atlassian.net',
        presetConfig: {
          types: [
            { type: 'feat', section: 'Features' },
            { type: 'fix', section: 'Bug Fixes' },
            { type: 'refactor', section: 'Miscellaneous' },
            { type: 'config', section: 'Miscellaneous' },
            { type: 'infra', section: 'Miscellaneous' },
            { type: 'maintenance', section: 'Miscellaneous' },
            { type: 'chore', section: 'Miscellaneous' },
          ]
        },
        writerOpts: {
          commitGroupsSort: (a, b) => {
            const order = ['Features', 'Bug Fixes', 'Miscellaneous'];
            return order.indexOf(a.title) - order.indexOf(b.title);
          },
          transform: (commit, context) => {
            const includeTypes = ['feat', 'maintenance', 'refactor', 'fix', 'config', 'infra', 'chore'];

            if (!includeTypes.includes(commit.type)) {
              return;
            }
            if (typeof commit.hash === `string`) {
              commit.shortHash = commit.hash.substring(0, 7)
            }

            if (commit.type === 'feat') {
              commit.type = 'Features'
            } else if (commit.type === 'fix') {
              commit.type = 'Bug Fixes'
            } else {
              commit.type = 'Miscellaneous'
            }

            return commit
          }
        },
      }
    ]
  ],
};
