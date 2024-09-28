package core

import (
	"encoding/json"
)

type TypeId int8

const (
	tpUnknown TypeId = iota
	tpString
	tpUint
	tpInt
)

var typeStringMapping = map[TypeId]string{
	tpUnknown: "unknown",
	tpString:  "string",
	tpUint:    "uint",
	tpInt:     "int",
}

func TypeToString(typeName TypeId) string {
	if str, found := typeStringMapping[typeName]; found {
		return str
	}

	return "unknown"
}

type Type struct {
	Name     string
	Size     uint8
	SubTypes map[string]Type
}

func InitType(name string) Type {
	return Type{
		Name:     name,
		SubTypes: make(map[string]Type),
	}
}

func (c Type) GetSize() uint8 {
	var size uint8 = c.Size

	for _, subType := range c.SubTypes {
		size += subType.GetSize()
	}

	return size
}

func (c Type) ToString() string {
	result, err := json.MarshalIndent(c, "", "\t")

	if err != nil {
		return "<failed to convert type to string>"
	}

	return string(result)
}

func (c Type) GenerateByteMap() string {
	return "todo"
}

type TypeCompatibility int8

const (
	Ok TypeCompatibility = iota
	Illegal
	LossOfInformation
)

func PerformAssignmentTypeCheck(to Type, from Type) TypeCompatibility {
	// int x = "something" is illegal
	if from.Name == "string" && (to.Name == "int" || to.Name == "uint") {
		return Illegal
	}

	// string(10) -> string(8) is allowed but COULD lead to loss of information
	if from.Size > to.Size {
		return LossOfInformation
	}

	return Ok
}

func GetElementaryType(valueType string, size uint8) Type {
	// TODO: uint/int distinction
	if valueType == "int" || valueType == "uint" {
		return Type{Name: "int", Size: size}
	}

	return Type{Name: "string", Size: size}
}

func GetImplicitType(node AstTreeNode) (Type, error) {
	switch node.Type {
	case ntString:
		return Type{Name: "string", Size: uint8(len(node.Value))}, nil
	case ntScalar:
		return Type{Name: "int", Size: uint8(len(node.Value))}, nil
	default:
		//return ElementaryType{}, fmt.Errorf("unable to determine implicit type")
		return Type{}, nil
	}
}

func GetType(node AstTreeNode) (Type, error) {
	cType := InitType(node.Value)

	for _, field := range node.Children {
		if field.Type == ntModifier {
			continue
		}

		if field.Type != ntStructure {
			elementaryType := GetElementaryType(field.Value, field.ValueSize)

			if len(field.Children) > 0 && field.Children[0].Type == ntType {
				elementaryType = GetElementaryType(field.Children[0].Value, field.Children[0].ValueSize)
			}

			cType.SubTypes[field.Value] = elementaryType
		} else if compositeField, err := GetType(field); err == nil {
			cType.SubTypes[field.Value] = compositeField
		} else {
			return Type{}, err
		}
	}

	// Trigger size update of complex types
	cType.Size = cType.GetSize()

	return cType, nil
}
