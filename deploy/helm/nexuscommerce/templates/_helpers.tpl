{{/* Common labels applied to every object. */}}
{{- define "nexuscommerce.labels" -}}
app.kubernetes.io/part-of: nexuscommerce
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end -}}

{{/* Per-service selector labels. Call with a dict of {name, root}. */}}
{{- define "nexuscommerce.selectorLabels" -}}
app.kubernetes.io/name: {{ .name }}
app.kubernetes.io/instance: {{ .root.Release.Name }}
{{- end -}}

{{/* Fully-qualified image reference for a service name. */}}
{{- define "nexuscommerce.image" -}}
{{- printf "%s/%s:%s" .root.Values.image.registry .name .root.Values.image.tag -}}
{{- end -}}
