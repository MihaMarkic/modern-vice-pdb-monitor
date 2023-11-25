#pragma once
#include "mapentity.h"
#include <c64/types.h>
#include "stats/statblock.h"

class Character : public MapEntity {
    public:
        Character(byte _xPos, byte _yPos, byte _image, byte _color);
    protected:
        StatBlock stats;
        AttributeBlock attributes;
};
#pragma compile("character.cpp")