import json
import os

mock_data_path = '/Users/ashkan/Desktop/projects/coreAxis/mockData.json'

if not os.path.exists(mock_data_path):
    print(f"Error: {mock_data_path} does not exist.")
    exit(1)

def fix_options(field):
    if "options" in field and isinstance(field["options"], list):
        for opt in field["options"]:
            if isinstance(opt, dict):
                # Map Id -> Value
                if "value" not in opt and "Id" in opt:
                    opt["value"] = str(opt["Id"])
                
                # Map Caption -> Label
                if "label" not in opt and "Caption" in opt:
                    opt["label"] = opt["Caption"]
                
                # Ensure value/label exist
                if "value" not in opt:
                    opt["value"] = "unknown" # Fallback
                
                # Ensure value is string
                if not isinstance(opt["value"], str):
                    opt["value"] = str(opt["value"])
                    
                if "label" not in opt:
                    opt["label"] = opt["value"]
                
                # Ensure label is string
                if not isinstance(opt["label"], str):
                    opt["label"] = str(opt["label"])

def fix_field(field):
    if "type" in field:
        ft = field["type"]
        if ft == "segmented":
            field["type"] = "Radio"
        elif ft == "tel":
            field["type"] = "Text" # Phone not in validator whitelist
        elif ft == "group":
            field["type"] = "Section"
        elif ft == "info":
            field["type"] = "Text"
            field["readOnly"] = True
        elif ft == "repeater":
            field["type"] = "Section"
            
    if "id" in field and ("name" not in field or not field["name"]):
        field["name"] = field["id"]
        
    if "label" not in field or not field["label"] or not field["label"].strip():
        # Use title if available, otherwise name, otherwise id
        if "title" in field and field["title"] and field["title"].strip():
            field["label"] = field["title"]
        elif "name" in field:
            field["label"] = field["name"]
        elif "id" in field:
            field["label"] = field["id"]
            
    fix_options(field)
    
    # Add dummy option if empty for Select/Radio/MultiSelect
    # Map types case-insensitive
    ft_lower = field.get("type", "").lower()
    if ft_lower in ["select", "multiselect", "radio", "checkbox"]:
        if "options" not in field or not field["options"]:
            field["options"] = [{"value": "dummy", "label": "Dynamic Option"}]

with open(mock_data_path, 'r', encoding='utf-8') as f:
    schema_content = f.read()

try:
    json_obj = json.loads(schema_content)
    
    # Fix version type mismatch (backend expects string, file has number)
    # Also force version to 1.0 as backend only supports 1.0, 1.1, 1.2
    if "version" in json_obj:
        json_obj["version"] = "1.0"
        
    # Fix missing title
    if "title" not in json_obj or not json_obj["title"]:
        json_obj["title"] = "فرم ورود اطلاعات بیمه عمر مهربان"
        
    # Polyfill 'fields' from 'steps' if missing (backend requires 'fields' at root)
    # Also fix field types
    if "steps" in json_obj and isinstance(json_obj["steps"], list):
        all_fields = []
        seen_ids = set()
        
        for step in json_obj["steps"]:
            if isinstance(step, dict) and "fields" in step and isinstance(step["fields"], list):
                for fld in step["fields"]:
                    fix_field(fld)
                    
                    # Filter out Sections/Repeaters from flattened list to avoid validation error "Unsupported field type Section"
                    if fld.get("type") == "Section":
                        continue
                        
                    # Deduplicate
                    if fld["id"] in seen_ids:
                        continue
                    seen_ids.add(fld["id"])
                    
                    all_fields.append(fld)
        
        if "fields" not in json_obj or not json_obj["fields"]:
            json_obj["fields"] = all_fields
    elif "fields" in json_obj and isinstance(json_obj["fields"], list):
        # ... logic for flat fields if steps not present ...
        # But we know steps are present.
        pass

    # Re-dump to ensure it is clean JSON string
    schema_string = json.dumps(json_obj, ensure_ascii=False)
except json.JSONDecodeError as e:
    print(f"Error decoding JSON: {e}")
    # If invalid JSON, maybe we still send it as string?
    # User said "mockData.json is the schema". It should be valid.
    schema_string = schema_content

payload = {
    "name": "life-mehraban-entry-v1",
    "title": "فرم ورود اطلاعات بیمه عمر مهربان",
    "description": "TejaratNo - Mehraban Life - v1",
    "tenantId": "tejaratno",
    "businessId": "life",
    "schemaJson": schema_string
}

with open('payload.json', 'w', encoding='utf-8') as f:
    json.dump(payload, f, ensure_ascii=False)

print("payload.json created successfully.")
