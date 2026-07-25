<?php

include("config.php");

$key = $_POST["key"];
$pseudo = $_POST["playerName"];

$query = "SELECT * FROM VerifKey WHERE keyGame='$key'";
$rowKey = $conn->query($query);

$UserExist = $conn->query("SELECT * FROM users WHERE username='$pseudo'");

if($rowKey->num_rows == 1){
	if($UserExist->num_rows == 1){
		
		echo "Error login";
		
	}else{
		echo "Login success";
	}
}else{
	echo "Error key";
}

?>