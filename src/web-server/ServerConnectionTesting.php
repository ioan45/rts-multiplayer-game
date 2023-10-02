<?php

if (isset($_GET['token']))
    echo($_GET['token']);
else
    echo('Token is not provided.');

?>