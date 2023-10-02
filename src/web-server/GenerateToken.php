<?php

function GenerateToken(int $length) : string
{
    $result = '';
    $possibleChars = str_shuffle('abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUWXYZ0123456789');
    $possibleCharsLength = strlen($possibleChars);
    for ($i = 0; $i < $length; ++$i)
        $result .= $possibleChars[random_int(0, $possibleCharsLength - 1)];
    return $result;
}

?>