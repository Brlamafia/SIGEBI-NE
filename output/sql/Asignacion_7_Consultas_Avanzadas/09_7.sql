-- EJERCICIO 9.7
SET SERVEROUTPUT ON
DEFINE p_apellido = ApellidoInexistente
DECLARE
  e_apellido_invalido EXCEPTION;
BEGIN
  DELETE FROM employees
   WHERE last_name = '&p_apellido';

  IF SQL%NOTFOUND THEN
    RAISE e_apellido_invalido;
  END IF;

  ROLLBACK;
EXCEPTION
  WHEN e_apellido_invalido THEN
    RAISE_APPLICATION_ERROR(-20089, 'Apellido ingresado invalido');
END;
/
