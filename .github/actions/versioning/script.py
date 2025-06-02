import json
import os
import requests
from git import Repo
import re

def load_config(json_path):
    if not os.path.exists(json_path):
        print(f"Config not found: {json_path}")
        exit(1)
    with open(json_path, "r") as f:
        return json.load(f)
    
def get_latest_matching_tag(repo, tag_format):
    pattern = tag_format.replace("{major}", r"(\d+)") \
                        .replace("{minor}", r"(\d+)") \
                        .replace("{patch}", r"(\d+)")
    regex_pattern = f"^{pattern}$"
    tags = repo.tags
    matching_tags = []

    for tag in tags:
        if re.match(regex_pattern, tag.name):
            matching_tags.append(tag)
    if not matching_tags:
        return None

    def extract_version(tag_name):
        match = re.match(regex_pattern, tag_name)
        if match:
            return match.groups()
        return (0, 0, 0)

    matching_tags.sort(key=lambda t: extract_version(t.name))
    return matching_tags[-1]

def determine_highest_priority(repo, config, github_token, latest_tag):
    priority_index = {"patch": 0, "minor": 1, "major": 2}
    type_priority = {}
    for level, keywords in config.items(): # Map keywords to priority level 
        for keyword in keywords:
            type_priority[keyword] = priority_index[level]
    highest_priority = -1
    commits = list(repo.iter_commits(f"{latest_tag}..HEAD"))
    for commit in commits:
        commit_hash = commit.hexsha
        try:
            response = requests.get(
                f"https://api.github.com/repos/fordpatsakorn/test-auto-versioning/commits/{commit_hash}/pulls",
                headers= {
                    "Accept": "application/vnd.github+json",
                    "Authorization": f"Bearer {github_token}",
                    "X-GitHub-Api-Version": "2022-11-28"
                }
            )
            response.raise_for_status()
            pulls = response.json()
            if pulls:
                ref = pulls[0]["head"]["ref"] # head.ref is the branch associated with the PR
                commit_type = ref.split("/")[0]
                priority = type_priority.get(commit_type)
                if priority is not None and priority > highest_priority:
                    highest_priority = priority
        except requests.HTTPError as http_err:
            print(f"HTTP error occurred: {http_err}")
    
    return highest_priority

def increment_version(latest_tag, version_type, tag_format):
    pattern = tag_format.replace("{major}", r"(\d+)") \
                        .replace("{minor}", r"(\d+)") \
                        .replace("{patch}", r"(\d+)")
    match = re.match(pattern, latest_tag.name)
    if match:
        major, minor, patch = map(int, match.groups())
        
        if version_type == 2:  # Major
            major += 1
            minor = 0
            patch = 0
        elif version_type == 1:  # Minor
            minor += 1
            patch = 0
        elif version_type == 0:  # Patch
            patch += 1
        return tag_format.format(major=major, minor=minor, patch=patch)
    return None

def main():
    action_path = os.getenv("ACTION_PATH")
    github_token = os.getenv("GITHUB_TOKEN")
    version_type = os.getenv("VERSION_TYPE")
    print(version_type)
    json_file = os.path.join(action_path, "config.json")
    config = load_config(json_file)
        
    repo = Repo("../../../")
    latest_tag = get_latest_matching_tag(repo,config.get("tag_format"))
    if (version_type == "auto"):
        highest_priority = determine_highest_priority(repo, config.get("keyword"), github_token, latest_tag)
    else:
        highest_priority = {"patch": 0, "minor": 1, "major": 2}.get(version_type, -1)
    new_version = increment_version(latest_tag, highest_priority, config.get("tag_format"))
    
    if highest_priority > -1:
        print(new_version)
    else:
        print("none")
        exit(1)

if __name__ == "__main__":
    main()