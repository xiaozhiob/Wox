import 'wox_plugin_setting_select.dart';

class PluginSettingValueTable {
  late String key;
  late String defaultValue;
  late bool enableFilter;
  late List<PluginSettingValueTableColumn> columns;

  PluginSettingValueTable.fromJson(Map<String, dynamic> json) {
    key = json['Key'];
    defaultValue = json['DefaultValue'];
    enableFilter = json['EnableFilter'];
    if (json['Columns'] != null) {
      columns = (json['Columns'] as List).map((e) => PluginSettingValueTableColumn.fromJson(e)).toList();
    } else {
      columns = [];
    }
  }
}

class PluginSettingValueTableColumn {
  late String key;
  late String label;
  late String tooltip;
  late int width;
  late String type;
  late List<PluginSettingValueSelectOption> selectOptions; // Only used when Type is PluginSettingValueTableColumnTypeSelect

  PluginSettingValueTableColumn.fromJson(Map<String, dynamic> json) {
    key = json['Key'];
    label = json['Label'];
    tooltip = json['Tooltip'];
    width = json['Width'];
    type = json['Type'];
    if (json['SelectOptions'] != null) {
      selectOptions = (json['SelectOptions'] as List).map((e) => PluginSettingValueSelectOption.fromJson(e)).toList();
    } else {
      selectOptions = [];
    }
  }
}
