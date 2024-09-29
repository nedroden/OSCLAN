package util

import "encoding/json"

func SerializeJson(data interface{}) ([]byte, error) {
	var bytes []byte
	var err error

	if bytes, err = json.MarshalIndent(data, "", "\t"); err != nil {
		return []byte{}, err
	}

	return bytes, nil
}
