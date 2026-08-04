-- EJERCICIO 9.4 - ACTUALIZACION EXITOSA
SET SERVEROUTPUT ON
DEFINE p_employee_id = 100
DEFINE p_porcentaje = 5
DECLARE
  e_empleado_no_existe EXCEPTION;
BEGIN
  UPDATE employees
     SET salary = salary * (1 + &p_porcentaje / 100)
   WHERE employee_id = &p_employee_id;

  IF SQL%NOTFOUND THEN
    RAISE e_empleado_no_existe;
  END IF;

  DBMS_OUTPUT.PUT_LINE('La informacion fue actualizada con exito...');
  ROLLBACK;
EXCEPTION
  WHEN e_empleado_no_existe THEN
    DBMS_OUTPUT.PUT_LINE('El empleado no existe. Revise el Identificador del empleado!');
END;
/
