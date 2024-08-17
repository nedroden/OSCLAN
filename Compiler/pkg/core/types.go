package core

import (
	"encoding/json"
	"fmt"
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

type ElementaryType struct {
	Name TypeId
	Size uint8
}

func (e ElementaryType) ToString() string {
	return fmt.Sprintf("%s(%d)", TypeToString(e.Name), e.Size)
}

type CompositeType struct {
	Name              string
	Types             map[string]ElementaryType
	SubCompositeTypes map[string]CompositeType
}

func InitCompositeType(name string) CompositeType {
	return CompositeType{
		Name:              name,
		Types:             make(map[string]ElementaryType),
		SubCompositeTypes: make(map[string]CompositeType),
	}
}

func (c CompositeType) GetSize() uint8 {
	var size uint8 = 0

	for _, subType := range c.Types {
		size += subType.Size
	}

	for _, subCompType := range c.SubCompositeTypes {
		size += subCompType.GetSize()
	}

	return size
}

func (c CompositeType) ToString() string {
	result, err := json.MarshalIndent(c, "", "\t")

	if err != nil {
		return "<failed to convert type to string>"
	}

	return string(result)
}

func (c CompositeType) GenerateByteMap() string {
	return "todo"
}

type TypeCompatibility int8

const (
	Ok TypeCompatibility = iota
	Illegal
	LossOfInformation
)

func PerformAssignmentTypeCheck(to ElementaryType, from ElementaryType) TypeCompatibility {
	// int x = "something" is illegal
	if from.Name == tpString && (to.Name == tpUint || to.Name == tpInt) {
		return Illegal
	}

	// string(10) -> string(8) is allowed but COULD lead to loss of information
	if from.Size > to.Size {
		return LossOfInformation
	}

	return Ok
}

func GetElementaryType(valueType string, size uint8) ElementaryType {
	// TODO: uint/int distinction
	if valueType == "int" || valueType == "uint" {
		return ElementaryType{Name: tpInt, Size: size}
	}

	return ElementaryType{Name: tpString, Size: size}
}

func GetImplicitType(node AstTreeNode) (ElementaryType, error) {
	switch node.Type {
	case ntString:
		return ElementaryType{Name: tpString, Size: uint8(len(node.Value))}, nil
	case ntScalar:
		return ElementaryType{Name: tpInt, Size: uint8(len(node.Value))}, nil
	default:
		//return ElementaryType{}, fmt.Errorf("unable to determine implicit type")
		return ElementaryType{}, nil
	}
}

func GetCompositeType(node AstTreeNode) (CompositeType, error) {
	cType := InitCompositeType(node.Value)

	for _, field := range node.Children {
		if field.Type != ntStructure {
			elementaryType := GetElementaryType(field.Value, field.ValueSize)

			if len(field.Children) > 0 && field.Children[0].Type == ntType {
				elementaryType = GetElementaryType(field.Children[0].Value, field.Children[0].ValueSize)
			}

			cType.Types[field.Value] = elementaryType
		} else if compositeField, err := GetCompositeType(field); err == nil {
			cType.SubCompositeTypes[field.Value] = compositeField
		} else {
			return CompositeType{}, err
		}
	}

	return cType, nil
}
