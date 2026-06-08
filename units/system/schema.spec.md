---
mbox_unit: 1
unit: schema
type: spec
version: 2
uses:
  spec: 1
---

# Schema Spec

Schema is a supporting spec that defines the inline schema language used to describe structured values in MBOX unit payloads.

This spec does not introduce units of type `schema`. A unit depends on this spec when its payload contains an inline schema governed by these rules.

Interface units use schemas for operation inputs and successful response results.

Box units use schemas for declared configuration values.

## Structured values

A structured value is one of:

1. `null`
2. A boolean
3. An integer
4. A number, including an integer where a number is accepted
5. A string
6. Binary data
7. An array of structured values
8. An object whose field names are strings and whose field values are structured values

Structured values are independent of transport encoding. A runtime or protocol spec may define how these values are serialized, including how binary data is represented in a serialized form.

## Schema form

A schema is a YAML mapping containing a required `type` field.

The defined type values are:

```yaml
type: <any | null | boolean | integer | number | string | binary | array | object>
```

Schema keys not defined for the selected type are invalid.

Schemas may be nested wherever this spec requires a schema value.

## Scalar schemas

`any` accepts every structured value and has no additional fields:

```yaml
type: any
```

`null` accepts only `null` and has no additional fields:

```yaml
type: null
```

`boolean` accepts only boolean values and has no additional fields:

```yaml
type: boolean
```

`integer` accepts only integer values. It may constrain the inclusive numeric range:

```yaml
type: integer
minimum: <number>
maximum: <number>
```

`number` accepts integer or non-integer numeric values. It may constrain the inclusive numeric range:

```yaml
type: number
minimum: <number>
maximum: <number>
```

For `integer` and `number`, `minimum` and `maximum` are optional. If both are present, `minimum` must not exceed `maximum`.

`string` accepts only strings. It may constrain length:

```yaml
type: string
minLength: <non-negative integer>
maxLength: <non-negative integer>
```

String length is measured in Unicode scalar values. `minLength` and `maxLength` are optional. If both are present, `minLength` must not exceed `maxLength`.

`binary` accepts only binary data. It may constrain byte length:

```yaml
type: binary
minBytes: <non-negative integer>
maxBytes: <non-negative integer>
```

`minBytes` and `maxBytes` are optional. If both are present, `minBytes` must not exceed `maxBytes`.

## Array schemas

An `array` schema must define the schema accepted for every item:

```yaml
type: array
items: <schema>
minItems: <non-negative integer>
maxItems: <non-negative integer>
```

`items` is required. `minItems` and `maxItems` are optional. If both are present, `minItems` must not exceed `maxItems`.

Every value in an accepted array must conform to `items`.

## Object schemas

An `object` schema must define named properties, required property names, and whether additional properties are accepted:

```yaml
type: object
properties:
  <property name>: <schema>
required:
  - <property name>
additionalProperties: <true | false>
```

`properties`, `required`, and `additionalProperties` are required.

Each property name must be unique. Every name in `required` must occur in `properties`.

A property listed in `required` must be present in an accepted object.

A property listed in `properties` but omitted from `required` is optional. When present, its value must conform to its schema.

When `additionalProperties` is `false`, an accepted object must not contain names absent from `properties`.

When `additionalProperties` is `true`, additional property values may be any structured value.

## Validation

A value conforms to a schema when it has the required type and satisfies every applicable constraint recursively.

Schema validation is used wherever a governing spec requires a structured value to satisfy an inline schema.

This spec defines conformance, not the runtime action to take when validation fails. Interface invocation or runtime protocol failure behavior and application configuration failure behavior belong to the specs that define those operations.

## Compatibility

A schema change is a payload change to the unit containing the inline schema and therefore increments that unit's version according to the kernel.

Changing a schema may affect units differently according to how the schema is used. For example, widening an interface operation input may affect providers, while narrowing an operation input may affect consumers.

The containing unit's governing spec determines which dependent units must be reevaluated for a changed schema.

Typical changes that require reevaluation include:

- changing a schema type;
- adding, removing, or changing constraints;
- adding or removing a required property;
- adding, removing, or changing a property schema;
- changing `additionalProperties`;
- changing an array item schema.

## Schema evaluation

When this schema spec changes, dependent specs and units must be reevaluated to determine whether their inline schemas remain valid and whether the contracts expressed by those schemas change.

If evaluation changes a dependent unit payload, that dependent unit version must be incremented.
