<?php

include("config.php");

$key = $_POST["key"];
$pseudo = $_POST["playerName"];
$password = md5($_POST["password"]);
$email = $_POST["email"];
$avatar = $_POST["idavatar"];

$query = "SELECT * FROM VerifKey WHERE keyGame='$key'";
$rowKey = $conn->query($query);

if($rowKey->num_rows == 1)
{
	$queryload = "INSERT INTO users (username, password, email, avatarID) VALUE ('$pseudo', '$password', '$email', '$avatar')";
				
	if ($conn->query($queryload) === TRUE) {
		
		echo "Ok";
	}
	else
	{
		echo "Error";
	}
}
else
{
	echo "Error key";
}

?>