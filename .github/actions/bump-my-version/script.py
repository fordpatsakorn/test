import json
import os
from urllib.error import HTTPError
import requests
from git import Repo

def load_priority_config(json_path):
    if not os.path.exists(json_path):
        print(f"Priority config not found: {json_path}")
        exit(1)
    with open(json_path, 'r') as f:
        return json.load(f)

def determine_highest_priority(repo, priority_config, headers):
    priority_index = {"patch": 0, "minor": 1, "major": 2}
    type_priority = {}
    
    for level, keywords in priority_config.items():
        for keyword in keywords:
            type_priority[keyword] = priority_index[level]

    highest_priority = -1
    latest_tag = repo.git.describe(tags=True, abbrev=0)
    commits = list(repo.iter_commits(f'{latest_tag}..HEAD'))
    print(commits)
    for commit in commits:
        commit_hash = commit.hexsha
        try:
            response = requests.get(
                f'https://api.github.com/repos/fordpatsakorn/test-auto-versioning/commits/{commit_hash}/pulls',
                headers=headers
            )
            response.raise_for_status()
            pulls = response.json()
            if pulls:
                ref = pulls[0]['head']['ref']
                commit_type = ref.split('/')[0]
                priority = type_priority.get(commit_type)
                if priority is not None and priority > highest_priority:
                    highest_priority = priority
        except HTTPError as http_err:
            print(f"HTTP error occurred: {http_err}")
    
    return highest_priority

def main():
    action_path = os.getenv('ACTION_PATH')
    github_token = "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"

    json_file = os.path.join('./', 'priority.json')
    priority_config = load_priority_config(json_file)
    
    headers = {
        'Accept': 'application/vnd.github+json',
        'Authorization': f'Bearer {github_token}',
        'X-GitHub-Api-Version': '2022-11-28'
    }
    
    repo = Repo('../../../')
    
    highest_priority = determine_highest_priority(repo, priority_config, headers)
    priority_labels = ["patch", "minor", "major"]
    
    if highest_priority > -1:
        print(priority_labels[highest_priority])
    else:
        print("none")

if __name__ == "__main__":
    main()