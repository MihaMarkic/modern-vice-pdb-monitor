#include "character.h"

Character::Character(byte _xPos, byte _yPos, byte _image, byte _color) : MapEntity(_xPos, _yPos, _image, _color)
{
    //temp
    stats.hp = stats.maxHp = 10;
}