import os
import fnmatch
import yaml

def load_config(yaml_file):
    """Load configuration from a YAML file."""
    with open(yaml_file, 'r', encoding='utf-8') as file:
        return yaml.safe_load(file)

def matches_blob_pattern(file_path, patterns):
    """Check if a file path matches any of the blob patterns."""
    rel_path = os.path.relpath(file_path)  # Get relative path for flexible matching
    for pattern in patterns:
        if fnmatch.fnmatch(rel_path, pattern) or fnmatch.fnmatch(os.path.basename(file_path), pattern):
            return True
    return False

def categorize_file(file_extension):
    """Assigns a category to a file based on its extension."""
    categories = {
        "source_code": [".cs", ".cpp", ".java", ".py"],
        "configurations": [".json", ".yaml", ".xml", ".csproj"],
        "documentation": [".md", ".txt"]
    }
    
    for category, extensions in categories.items():
        if file_extension in extensions:
            return category.upper().replace("_", " ")
    
    return "OTHER FILES"

def concatenate_files(config):
    source_dir = config['source_directory']
    output_file = config['output_file']
    extensions = config['file_extensions']
    exclude_files = config['exclude_files']  # Blob paths for exclusions

    with open(output_file, 'w', encoding='utf-8') as out_file:
        categorized_files = {}  # Store files per category

        for root, _, files in os.walk(source_dir):
            for file in files:
                if any(file.endswith(ext) for ext in extensions):
                    file_path = os.path.join(root, file)

                    # Skip files matching blob patterns
                    if matches_blob_pattern(file_path, exclude_files):
                        continue
                    
                    file_category = categorize_file(os.path.splitext(file)[1])
                    categorized_files.setdefault(file_category, []).append((file_path, file))

        # Write organized content
        for category, files in categorized_files.items():
            out_file.write(f"\n## {category}\n")
            for file_path, file_name in files:
                out_file.write(f"\n### FILE START: {file_name}\n")
                out_file.write(f"# File Path: {file_path}\n")

                with open(file_path, 'r', encoding='utf-8') as in_file:
                    out_file.write(in_file.read() + "\n")

                out_file.write(f"\n### FILE END: {file_name}\n")

if __name__ == "__main__":
    config_file = "config.yaml"  # Change to your actual config path
    config = load_config(config_file)
    concatenate_files(config)
